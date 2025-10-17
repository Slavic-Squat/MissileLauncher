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
            #region Parts
            private IMyMotorStator _azimuthRotor;
            private IMyMotorStator _elevationRotor;
            private CameraArray _cameraArray;
            #endregion

            #region State Info
            private float _maxRaycastDistance;
            private double _lastRunTime;
            private Matrix _referenceMatrix;
            private bool _azimuthRotorInverted;
            private bool _elevationRotorInverted;
            private float _azimuthRotorAngle;
            private float _elevationRotorAngle;
            private double _lastUniqueDetectionTime;
            private int _matchingDetectionCounter;
            private MyDetectedEntityInfo _previouslyDetectedEntity;
            #endregion

            #region Controllers
            private PIDControl _azimuthPID;
            private PIDControl _elevationPID;
            #endregion

            #region Properties
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
            public event Func<TargetingLaser, bool> SyncRequested;
            public event Func<IControllable, bool> RequestRelease;
            #endregion

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
                _azimuthRotor = GTS.GetBlockWithName($"Azimuth Rotor [{ID}]") as IMyMotorStator;
                if (_azimuthRotor == null)
                {
                    throw new Exception($"No Azimuth Rotor Found For Targeting Laser [{ID}]");
                }
                _elevationRotor = GTS.GetBlockWithName($"Elevation Rotor [{ID}]") as IMyMotorStator;
                if (_elevationRotor == null)
                {
                    throw new Exception($"No Elevation Rotor Found For Targeting Laser [{ID}]");
                }
            }

            private void Init()
            {
                _azimuthRotorInverted = _azimuthRotor.CustomData.Contains("Inverted");
                _elevationRotorInverted = _elevationRotor.CustomData.Contains("Inverted");

                _cameraArray = new CameraArray(0, _maxRaycastDistance);
                _azimuthPID = new PIDControl(25, 2, 0.1f);
                _elevationPID = new PIDControl(25, 2, 0.1f);
            }

            public void Run(double time)
            {
                Time = time;
                if (_lastRunTime == 0)
                    _lastRunTime = time;

                _azimuthRotorAngle = _azimuthRotorInverted ? -_azimuthRotor.Angle : _azimuthRotor.Angle;
                _elevationRotorAngle = _elevationRotorInverted ? -_elevationRotor.Angle : _elevationRotor.Angle;

                Matrix H0 = _azimuthRotor.WorldMatrix;
                Matrix H1 = Matrix.CreateRotationY(_azimuthRotorAngle);
                Matrix H2 = Matrix.CreateRotationX(_elevationRotorAngle);
                H2.Translation = new Vector3(0, 3, 0);

                _referenceMatrix = H2 * H1 * H0;

                _cameraArray.Update(time);

                if (!IsUnderControl && !HasTarget)
                {
                    MoveLaser(0, 0);
                }

                if (HasTarget && !ManualOverride)
                {
                    AutoTrack();
                }

                _lastRunTime = time;
            }

            private void AutoTrack()
            {
                float timeDeltaSeconds = (float)(Time - _lastRunTime);

                float timeSinceLastDetection = (float)(Time - Target.TimeRecorded);
                Vector3 estimatedTargetPosition = Target.Position + Target.Velocity * timeSinceLastDetection;
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
                    var azimuthInput = _azimuthPID.Run(azimuthError, timeDeltaSeconds) / Sensitivity;
                    var elevationInput = _elevationPID.Run(elevationError, timeDeltaSeconds) / Sensitivity;

                    MoveLaser(azimuthInput, elevationInput);
                    FireLaser(estimatedTargetPosition, 0.1f);
                }
            }

            private void MoveLaser(float azimuthInput, float elevationInput)
            {
                _elevationRotor.TargetVelocityRad = _elevationRotorInverted ? -elevationInput * Sensitivity : elevationInput * Sensitivity;
                _azimuthRotor.TargetVelocityRad = _azimuthRotorInverted ? -azimuthInput * Sensitivity : azimuthInput * Sensitivity;
            }

            private void ForgetTarget()
            {
                Target = default(EntityInfoExt);
                _matchingDetectionCounter = 0;
            }

            private bool FireLaser(Vector3 raycastTarget, float overshoot)
            {
                if (!_cameraArray.CanScan(raycastTarget, 0.1f))
                    return false;

                var raycastResult = _cameraArray.Raycast(raycastTarget, 0.1f);

                if (!raycastResult.IsEmpty())
                {
                    if (HasTarget && raycastResult.EntityId == Target.EntityID)
                    {
                        var freshTarget = new EntityInfoExt(raycastResult, Time);
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
                            Target = new EntityInfoExt(raycastResult, Time);
                        }
                    }
                }
                return true;
            }

            public bool Control(UserInput input, object caller)
            {
                if (!IsUnderControl || IsControlPaused || !ReferenceEquals(Controller, caller))
                    return false;

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
                    return false;
                }
                else if (input.CRelease)
                {
                    ForgetTarget();
                }
                return true;
            }

            public bool GiveControl(IController controller)
            {
                if (controller == null || IsUnderControl || ReferenceEquals(Controller, controller))
                {
                    return false;
                }
                Controller = controller;
                ResumeControl();
                return true;
            }

            public bool RevokeControl(IController controller)
            {
                if (controller == null || !IsUnderControl || !ReferenceEquals(Controller, controller))
                {
                    return false;
                }
                Controller = null;
                PauseControl();
                MoveLaser(0, 0);
                return true;
            }

            private bool RevokeControl()
            {
                return RequestRelease?.Invoke(this) ?? false;
            }

            public bool PauseControl()
            {
                IsControlPaused = true;
                return true;
            }

            public bool ResumeControl()
            {
                IsControlPaused = false;
                return true;
            }

            public override string ToString()
            {
                return $"LASER [{ID}]\n----------------\nSTATUS: {(IsUnderControl ? "CONTROLLED" : "FREE")}\nLOCKED: {(HasTarget ? "YES" : "NO")}\nMAX DIST: {MaxRaycastDistance:0} m";
            }
        }
    }
}
