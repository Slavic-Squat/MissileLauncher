using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class TargetingLaser : IControllable
        {
            private Rotor _azimuthRotor;
            private Rotor _elevationRotor;
            private CameraArray _cameraArray;

            private float _maxRaycastDistance;
            private MatrixD _referenceMatrix;
            private double _lastUniqueDetectionTime;
            private int _matchingDetectionCounter;
            private MyDetectedEntityInfo _previouslyDetectedEntity;

            private PIDControl _azimuthPID;
            private PIDControl _elevationPID;

            public int ID { get; private set; }
            public double Time { get; private set; }
            public IController Controller { get; private set; }
            public bool IsControlPaused { get; private set; } = true;
            public bool IsUnderControl => Controller != null;
            public bool HasTarget => Target.IsValid;
            public float MaxRaycastDistance
            {
                get
                {
                    return _maxRaycastDistance;
                }
                set
                {
                    _maxRaycastDistance = value;
                    _cameraArray.MaxRaycastDistance = value;
                }
            }
            public float Sensitivity { get; set; }
            public bool ManualOverride { get; set; }
            public EntityInfoExt Target {  get; private set; }
            public event Action<TargetingLaser> SyncRequested;
            public event Action<IControllable> RequestRelease;

            public TargetingLaser(int id, float sensitivity = 0.05f, float maxRaycastDistance = 5000, bool manualOverride = false)
            {
                ID = id;
                Sensitivity = sensitivity;
                _maxRaycastDistance = maxRaycastDistance;
                ManualOverride = manualOverride;

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                _azimuthRotor = new Rotor($"LASER {ID} AZIMUTH ROTOR");
                _elevationRotor = new Rotor($"LASER {ID} ELEVATION ROTOR");
            }

            private void Init()
            {
                _cameraArray = new CameraArray($"LASER {ID} CAMERA ARRAY", _maxRaycastDistance);
                _azimuthPID = new PIDControl(25, 2, 0.1f);
                _elevationPID = new PIDControl(25, 2, 0.1f);
            }

            public void Run(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }

                _cameraArray.Update(time);

                MatrixD H0 = _azimuthRotor.RotorBlock.WorldMatrix;
                MatrixD H1 = MatrixD.CreateRotationY(_azimuthRotor.CurrentAngle);
                MatrixD H2 = MatrixD.CreateRotationX(_elevationRotor.CurrentAngle);
                H2.Translation = new Vector3D(0, 3, 0);

                _referenceMatrix = H2 * H1 * H0;

                if (!IsUnderControl && !HasTarget)
                {
                    MoveLaser(0, 0);
                }

                if (HasTarget && !ManualOverride)
                {
                    AutoTrack(time);
                }
                Time = time;
            }

            private void AutoTrack(double time)
            {
                double timeDeltaSeconds = time - Time;
                double globalTime = SystemCoordinator.GlobalTime;

                double timeSinceLastDetection = globalTime - Target.TimeRecorded;
                Vector3D estimatedTargetPosition = Target.Position + Target.Velocity * timeSinceLastDetection;
                double estimatedTargetDistance = (estimatedTargetPosition - _referenceMatrix.Translation).Length();

                if (estimatedTargetDistance > MaxRaycastDistance * 0.8 || timeSinceLastDetection > 5)
                {
                    ForgetTarget();
                }

                if (HasTarget)
                {
                    Vector3D estimatedTargetPosLocal = Vector3D.TransformNormal(estimatedTargetPosition - _referenceMatrix.Translation, MatrixD.Transpose(_referenceMatrix));
                    Vector3D estimatedTargetDirLocal = estimatedTargetDistance == 0 ? Vector3D.Zero : estimatedTargetPosLocal / estimatedTargetDistance;
                    double azimuthError = Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z);
                    double elevationError = Math.Asin(estimatedTargetDirLocal.Y);
                    float azimuthInput = _azimuthPID.Run((float)azimuthError, (float)timeDeltaSeconds) / Sensitivity;
                    float elevationInput = _elevationPID.Run((float)elevationError, (float)timeDeltaSeconds) / Sensitivity;

                    MoveLaser(azimuthInput, elevationInput);
                    FireLaser(estimatedTargetPosition, 0.1f);
                }
            }

            private void MoveLaser(float azimuthInput, float elevationInput)
            {
                _elevationRotor.Velocity = elevationInput * Sensitivity;
                _azimuthRotor.Velocity = azimuthInput * Sensitivity;
            }

            private void ForgetTarget()
            {
                Target = default(EntityInfoExt);
                _matchingDetectionCounter = 0;
            }

            private void FireLaser(Vector3D raycastTarget, float overshoot)
            {
                double globalTime = SystemCoordinator.GlobalTime;

                if (!_cameraArray.CanScan(raycastTarget, 0.1f))
                    return;

                var raycastResult = _cameraArray.Raycast(raycastTarget, 0.1f);

                if (!raycastResult.IsEmpty())
                {
                    if (!HasTarget)
                    {
                        if (raycastResult.EntityId == _previouslyDetectedEntity.EntityId)
                        {
                            _matchingDetectionCounter += 1;
                        }
                        else
                        {
                            _lastUniqueDetectionTime = Time;
                            _matchingDetectionCounter = 0;
                        }

                        _previouslyDetectedEntity = raycastResult;

                        double timeSinceLastUniqueDetection = Time - _lastUniqueDetectionTime;
                        if (timeSinceLastUniqueDetection > 2 && _matchingDetectionCounter >= 3)
                        {
                            Target = new EntityInfoExt(raycastResult, globalTime);
                        }
                    }
                    else if (raycastResult.EntityId == Target.EntityID)
                    {
                        Target = new EntityInfoExt(raycastResult, globalTime);
                    }
                }
            }

            public void Control(UserInput input, object caller)
            {
                if (!IsUnderControl || IsControlPaused || !ReferenceEquals(Controller, caller))
                    return;

                if (input.QRelease)
                {
                    SyncRequested?.Invoke(this);
                }

                if (!HasTarget || ManualOverride)
                {
                    MoveLaser(-input.MouseInput.Y, -input.MouseInput.X);

                    if (input.SpacePress)
                    {
                        Vector3D raycastTarget = _referenceMatrix.Forward * MaxRaycastDistance * 0.9f + _referenceMatrix.Translation;
                        FireLaser(raycastTarget, 0f);
                    }
                }
                if (input.CHeldAndReleased)
                {
                    RevokeControl();
                    return;
                }
                else if (input.CRelease)
                {
                    ForgetTarget();
                }
            }

            public void GiveControl(IController controller)
            {
                if (controller == null || IsUnderControl || ReferenceEquals(Controller, controller))
                {
                    return;
                }
                Controller = controller;
                ResumeControl(controller);
            }

            public void RevokeControl(IController controller)
            {
                if (controller == null || !IsUnderControl || !ReferenceEquals(Controller, controller))
                {
                    return;
                }
                Controller = null;
                PauseControl(controller);
            }

            private void RevokeControl()
            {
                RequestRelease?.Invoke(this);
            }

            public void PauseControl(IController controller)
            {
                if (controller == null || !IsUnderControl || !ReferenceEquals(Controller, controller))
                {
                    return;
                }
                MoveLaser(0, 0);
                IsControlPaused = true;
            }

            public void ResumeControl(IController controller)
            {
                if (controller == null || !IsUnderControl || !ReferenceEquals(Controller, controller))
                {
                    return;
                }
                IsControlPaused = false;
            }

            public string GetOverview()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[LASER {ID}]");
                sb.AppendLine($"  STATUS: {(IsUnderControl ? "CONTROLLED" : "FREE")}");
                sb.AppendLine($"  LOCKED: {(HasTarget ? "YES" : "NO")}");
                sb.AppendLine($"  RNG: {MaxRaycastDistance:F0} m");

                return sb.ToString();
            }
        }
    }
}
