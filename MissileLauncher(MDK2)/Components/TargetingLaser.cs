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
        public class TargetingLaser
        {
            #region Parts
            private Program _program;
            private IMyMotorStator _azimuthRotor;
            private IMyMotorStator _elevationRotor;
            private CameraArray _cameraArray;
            private ControlStation _controlStation;
            #endregion

            #region State Info
            private DateTime _lastRunTime;
            private Matrix _referenceMatrix;
            private bool _azimuthRotorInverted;
            private bool _elevationRotorInverted;
            private float _azimuthRotorAngle;
            private float _elevationRotorAngle;
            private DateTime _lastUniqueDetectionTime;
            private int _matchingDetectionCounter;
            private MyDetectedEntityInfo _previouslyDetectedEntity;
            #endregion

            #region Controllers
            private PIDControl _azimuthPID;
            private PIDControl _elevationPID;
            #endregion

            #region Properties
            public int ID { get; private set; }
            public bool IsUnderControl { get; private set; }
            public float MaxRaycastDistance
            {
                get
                {
                    return _cameraArray.MaxRaycastDistance;
                }
                set
                {
                    _cameraArray.MaxRaycastDistance = value;
                }
            }
            public float Sensitivity { get; set; }
            public bool ManualOverride { get; set; }
            public EntityInfo Target {  get; private set; }
            #endregion

            public TargetingLaser(Program program, int id, float sensitivity = 0.05f, float maxRaycastDistance = 5000, bool manualOverride = false)
            {
                _program = program;
                ID = id;
                Sensitivity = sensitivity;

                TryGetBlocks();
                Init();

                _cameraArray = new CameraArray(_program, 0, maxRaycastDistance);
                _azimuthPID = new PIDControl(25, 2, 0.1f);
                _elevationPID = new PIDControl(25, 2, 0.1f);
            }

            private bool TryGetBlocks()
            {
                try
                {
                    _azimuthRotor = _program.GridTerminalSystem.GetBlockWithName($"Azimuth Rotor [{ID}]") as IMyMotorStator;
                    if (_azimuthRotor == null)
                    {
                        throw new Exception();
                    }
                    _elevationRotor = _program.GridTerminalSystem.GetBlockWithName($"Elevation Rotor [{ID}]") as IMyMotorStator;
                    if (_elevationRotor == null)
                    {
                        throw new Exception();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _program.Echo("Error in TargetingLaser construction");
                    return false;
                }
            }

            private void Init()
            {
                _azimuthRotorInverted = _azimuthRotor.CustomData.Contains("Inverted");
                _elevationRotorInverted = _elevationRotor.CustomData.Contains("Inverted");
            }

            public void Run(DateTime time)
            {
                if (_lastRunTime == default(DateTime))
                    _lastRunTime = time;

                _azimuthRotorAngle = _azimuthRotorInverted ? -_azimuthRotor.Angle : _azimuthRotor.Angle;
                _elevationRotorAngle = _elevationRotorInverted ? -_elevationRotor.Angle : _elevationRotor.Angle;

                Matrix H0 = _azimuthRotor.WorldMatrix;
                Matrix H1 = Matrix.CreateRotationY(_azimuthRotorAngle);
                Matrix H2 = Matrix.CreateRotationX(_elevationRotorAngle);
                H2.Translation = new Vector3(0, 3, 0);

                _referenceMatrix = H2 * H1 * H0;

                MoveLaser(0, 0);

                if (IsUnderControl && _controlStation != null)
                {
                    ControlLaser(time);
                }

                if (Target != null && ManualOverride == false)
                {
                    AutoTrack(time);
                }

                _lastRunTime = time;
            }

            private void AutoTrack(DateTime time)
            {
                float timeDeltaMiliseconds = (float)(time - _lastRunTime).TotalMilliseconds;
                float timeDeltaSeconds = (float)(time - _lastRunTime).TotalSeconds;

                TimeSpan timeSinceLastDetection = time - Target.TimeRecorded;
                Vector3 estimatedTargetPosition = Target.Position + Target.Velocity * (float)timeSinceLastDetection.TotalSeconds;
                float estimatedTargetDistance = (estimatedTargetPosition - _referenceMatrix.Translation).Length();

                if (estimatedTargetDistance > MaxRaycastDistance * 0.8f || timeSinceLastDetection.TotalSeconds > 5)
                {
                    ForgetTarget();
                }

                if (Target != null)
                {
                    Vector3 estimatedTargetDirLocal = Vector3.Normalize(Vector3.TransformNormal(estimatedTargetPosition - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix)));
                    float azimuthError = (float)Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z);
                    float elevationError = (float)Math.Asin(estimatedTargetDirLocal.Y);
                    var azimuthInput = _azimuthPID.Run(azimuthError, timeDeltaSeconds) / Sensitivity;
                    var elevationInput = _elevationPID.Run(elevationError, timeDeltaSeconds) / Sensitivity;

                    MoveLaser(azimuthInput, elevationInput);
                    FireLaser(time, estimatedTargetPosition, 0.1f);
                }
            }

            private void MoveLaser(float azimuthInput, float elevationInput)
            {
                _elevationRotor.TargetVelocityRad = _elevationRotorInverted ? -elevationInput * Sensitivity : elevationInput * Sensitivity;
                _azimuthRotor.TargetVelocityRad = _azimuthRotorInverted ? -azimuthInput * Sensitivity : azimuthInput * Sensitivity;
            }

            private void ForgetTarget()
            {
                Target = null;
                _matchingDetectionCounter = 0;
            }

            private void FireLaser(DateTime time, Vector3 raycastTarget, float overshoot)
            {
                if (!_cameraArray.CanScan(raycastTarget, time, 0.1f))
                    return;

                var raycastResult = _cameraArray.Raycast(raycastTarget, time, 0.1f);

                if (!raycastResult.IsEmpty())
                {
                    if (Target != null && raycastResult.EntityId == Target.EntityID)
                    {
                        Target.UpdateFromRaycast(raycastResult, time);
                    }

                    else if (Target == null)
                    {
                        if (raycastResult.EntityId == _previouslyDetectedEntity.EntityId)
                        {
                            _matchingDetectionCounter += 1;
                        }
                        else
                        {
                            _lastUniqueDetectionTime = time;
                            _matchingDetectionCounter = 0;
                        }

                        _previouslyDetectedEntity = raycastResult;

                        TimeSpan timeSinceLastUniqueDetection = time - _lastUniqueDetectionTime;
                        if (timeSinceLastUniqueDetection.TotalSeconds > 2 && _matchingDetectionCounter >= 3)
                        {
                            Target = EntityInfo.CreateFromRaycast(raycastResult, time);
                        }
                    }
                }
            }

            private void ControlLaser(DateTime time)
            {
                if (_controlStation == null)
                    return;

                UserInput userInput = _controlStation.UserInput;

                MoveLaser(-userInput.MouseInput.Y, -userInput.MouseInput.X);

                if (userInput.SpacePress)
                {
                    if (Target == null)
                    {
                        Vector3 raycastTarget = _referenceMatrix.Forward * MaxRaycastDistance + _referenceMatrix.Translation;
                        FireLaser(time, raycastTarget, 0f);
                    }
                }
                else if (userInput.CRelease)
                {
                    ForgetTarget();
                }
            }

            public void TakeControl(ControlStation controlStation)
            {
                _controlStation = controlStation;
                IsUnderControl = true;
            }

            public void ReleaseControl()
            {
                _controlStation = null;
                IsUnderControl = false;
            }
        }
    }
}
