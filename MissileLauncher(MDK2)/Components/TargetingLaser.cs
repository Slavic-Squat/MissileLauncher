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
            private Matrix _referenceMatrix;
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
                _azimuthRotor = new Rotor($"Laser {ID} Azimuth Rotor");
                _elevationRotor = new Rotor($"Laser {ID} Elevation Rotor");
            }

            private void Init()
            {
                _cameraArray = new CameraArray(0, _maxRaycastDistance);
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

                Matrix H0 = _azimuthRotor.RotorBlock.WorldMatrix;
                Matrix H1 = Matrix.CreateRotationY(_azimuthRotor.CurrentAngle);
                Matrix H2 = Matrix.CreateRotationX(_elevationRotor.CurrentAngle);
                H2.Translation = new Vector3(0, 3, 0);

                _referenceMatrix = H2 * H1 * H0;

                _cameraArray.Update(time);

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
                Vector3 estimatedTargetPosition = Target.Position + Target.Velocity * (float)timeSinceLastDetection;
                float estimatedTargetDistance = (estimatedTargetPosition - _referenceMatrix.Translation).Length();

                if (estimatedTargetDistance > MaxRaycastDistance * 0.8f || timeSinceLastDetection > 5f)
                {
                    ForgetTarget();
                }

                if (HasTarget)
                {
                    Vector3 estimatedTargetPosLocal = Vector3.TransformNormal(estimatedTargetPosition - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix));
                    Vector3 estimatedTargetDirLocal = estimatedTargetDistance == 0 ? Vector3.Zero : estimatedTargetPosLocal / estimatedTargetDistance;
                    float azimuthError = (float)Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z);
                    float elevationError = (float)Math.Asin(estimatedTargetDirLocal.Y);
                    var azimuthInput = _azimuthPID.Run(azimuthError, (float)timeDeltaSeconds) / Sensitivity;
                    var elevationInput = _elevationPID.Run(elevationError, (float)timeDeltaSeconds) / Sensitivity;

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

            private void FireLaser(Vector3 raycastTarget, float overshoot)
            {
                double globalTime = SystemCoordinator.GlobalTime;

                if (!_cameraArray.CanScan(raycastTarget, 0.1f))
                    return;

                var raycastResult = _cameraArray.Raycast(raycastTarget, 0.1f);

                if (!raycastResult.IsEmpty())
                {
                    if (HasTarget && raycastResult.EntityId == Target.EntityID)
                    {
                        var freshTarget = new EntityInfoExt(raycastResult, globalTime);
                        Target = Target.Merge(freshTarget);
                    }

                    else if (!HasTarget)
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

                        float timeSinceLastUniqueDetection = (float)(Time - _lastUniqueDetectionTime);
                        if (timeSinceLastUniqueDetection > 2f && _matchingDetectionCounter >= 3)
                        {
                            Target = new EntityInfoExt(raycastResult, globalTime);
                        }
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
                        Vector3 raycastTarget = _referenceMatrix.Forward * MaxRaycastDistance * 0.9f + _referenceMatrix.Translation;
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

            public override string ToString()
            {
                return $"LASER [{ID}]\n----------------\nSTATUS: {(IsUnderControl ? "CONTROLLED" : "FREE")}\nLOCKED: {(HasTarget ? "YES" : "NO")}\nMAX DIST: {MaxRaycastDistance:0} m";
            }
        }
    }
}
