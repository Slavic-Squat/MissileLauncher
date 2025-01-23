using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            private IMyMotorStator _azimuthRotor;
            private IMyMotorStator _elevationRotor;
            private IMyShipController _controller;
            private List<IMyCameraBlock> _cameraArray = new List<IMyCameraBlock>();
            #endregion

            #region State Info
            private Matrix _referenceMatrix;
            private bool _azimuthRotorInverted;
            private bool _elevationRotorInverted;
            private float _maxTargetDistance;
            private float _azimuthRotorAngle;
            private float _azimuthError;
            private float _elevationRotorAngle;
            private float _elevationError;
            private int _raycastCounter;
            private float _totalAvailRaycastDistance;
            private DateTime _lastUniqueDetectionTime;
            private int _matchingDetectionCounter;
            private MyDetectedEntityInfo _detectedEntity;
            private MyDetectedEntityInfo _previouslyDetectedEntity;            
            #endregion

            #region Controllers
            private PIDControl _azimuthPID;
            private PIDControl _elevationPID;
            #endregion

            #region Properties
            public Program Program { get; private set; }
            public int ID { get; private set; }
            public float MaxRaycastDistance { get; set; }
            public float RaycastDistanceGrowthSpeed { get; set; }
            public float Sensitivity { get; set; }
            public bool ManualOverride { get; set; }
            public TargetInfo Target {  get; private set; }
            #endregion

            public TargetingLaser(Program program, int id, IMyShipController controller, float sensitivity = 0.05f, float maxRaycastDistance = 5000, float raycastDistanceGrowthSpeed = 200, bool manualOverride = false)
            {
                Program = program;
                ID = id;
                Sensitivity = sensitivity;
                MaxRaycastDistance = maxRaycastDistance;
                _maxTargetDistance = MaxRaycastDistance * 0.8f;
                RaycastDistanceGrowthSpeed = raycastDistanceGrowthSpeed;
                _controller = controller;

                TryGetBlocks();
                Init();

                _azimuthPID = new PIDControl(25, 2, 0.1f);
                _elevationPID = new PIDControl(25, 2, 0.1f);
            }

            public bool TryGetBlocks()
            {
                try
                {
                    _azimuthRotor = Program.GridTerminalSystem.GetBlockWithName($"Azimuth Rotor [{ID}]") as IMyMotorStator;
                    if (_azimuthRotor == null)
                    {
                        throw new Exception();
                    }
                    _elevationRotor = Program.GridTerminalSystem.GetBlockWithName($"Elevation Rotor [{ID}]") as IMyMotorStator;
                    if (_elevationRotor == null)
                    {
                        throw new Exception();
                    }
                    var CameraGroup = Program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array [{ID}]");
                    if (CameraGroup == null)
                    {
                        throw new Exception();
                    }
                    CameraGroup.GetBlocksOfType<IMyCameraBlock>(_cameraArray);
                    return true;
                }
                catch (Exception ex)
                {
                    Program.Echo("Error in TargetingLaser construction");
                    return false;
                }
            }

            public void Init()
            {
                foreach (IMyCameraBlock camera in _cameraArray)
                {
                    camera.EnableRaycast = true;
                    _totalAvailRaycastDistance += (float)camera.AvailableScanRange;
                }

                _azimuthRotorInverted = _azimuthRotor.CustomData.Contains("Inverted");
                _elevationRotorInverted = _elevationRotor.CustomData.Contains("Inverted");
            }

            public void Run(DateTime time)
            {
                float timeDeltaMiliseconds = (float)Program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                float timeDeltaSeconds = (float)Program.Runtime.TimeSinceLastRun.TotalSeconds;

                _totalAvailRaycastDistance += 2 * timeDeltaMiliseconds * _cameraArray.Count;

                _azimuthRotorAngle = _azimuthRotorInverted ? -_azimuthRotor.Angle : _azimuthRotor.Angle;
                _elevationRotorAngle = _elevationRotorInverted ? -_elevationRotor.Angle : _elevationRotor.Angle;

                Matrix H0 = _azimuthRotor.WorldMatrix;
                Matrix H1 = Matrix.CreateRotationY(_azimuthRotorAngle);
                Matrix H2 = Matrix.CreateRotationX(_elevationRotorAngle);
                H2.Translation = new Vector3(0, 3, 0);

                _referenceMatrix = H2 * H1 * H0;

                TimeSpan timeSinceLastTargetDetection = TimeSpan.Zero;
                Vector3 estimatedTargetPos = Vector3.Zero;
                float estimatedTargetDistance = 0;

                if (!Target.IsEmpty())
                {
                    timeSinceLastTargetDetection = time - Target.TimeRecorded;
                    estimatedTargetPos = Target.Position + Target.Velocity * (float)timeSinceLastTargetDetection.TotalSeconds;
                    estimatedTargetDistance = (estimatedTargetPos - _referenceMatrix.Translation).Length();

                    if (_controller.MoveIndicator.Y == -1 || estimatedTargetDistance > _maxTargetDistance || timeSinceLastTargetDetection.TotalSeconds > 5)
                    {
                        Target = new TargetInfo();
                        _matchingDetectionCounter = 0;
                    }
                }

                if (!Target.IsEmpty())
                {
                    if (ManualOverride == false)
                    {
                        Vector3 estimatedTargetDirLocal = Vector3.Normalize(Vector3.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix)));
                        _azimuthError = (float)Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z);
                        _elevationError = (float)Math.Asin(estimatedTargetDirLocal.Y);

                        _azimuthRotor.TargetVelocityRad = _azimuthRotorInverted ? -_azimuthPID.Run(_azimuthError, timeDeltaSeconds) : _azimuthPID.Run(_azimuthError, timeDeltaSeconds);
                        _elevationRotor.TargetVelocityRad = _elevationRotorInverted ? -_elevationPID.Run(_elevationError, timeDeltaSeconds) : _elevationPID.Run(_elevationError, timeDeltaSeconds);
                    }
                    else if (ManualOverride == true)
                    {
                        _elevationRotor.TargetVelocityRad = _elevationRotorInverted ? _controller.RotationIndicator.X * Sensitivity : -_controller.RotationIndicator.X * Sensitivity;
                        _azimuthRotor.TargetVelocityRad = _azimuthRotorInverted ? _controller.RotationIndicator.Y * Sensitivity : -_controller.RotationIndicator.Y * Sensitivity;
                    }
                }

                else
                {
                    _elevationRotor.TargetVelocityRad = _elevationRotorInverted ? _controller.RotationIndicator.X * Sensitivity : -_controller.RotationIndicator.X * Sensitivity;
                    _azimuthRotor.TargetVelocityRad = _azimuthRotorInverted ? _controller.RotationIndicator.Y * Sensitivity : -_controller.RotationIndicator.Y * Sensitivity;
                }

                float baseAvailRaycastDistance = 2 * MaxRaycastDistance * _cameraArray.Count;

                if (_totalAvailRaycastDistance >= baseAvailRaycastDistance && ((!Target.IsEmpty() && ManualOverride == false) || _controller.MoveIndicator.Y == 1))
                {
                    Vector3 cameraPos = _cameraArray[_raycastCounter].GetPosition();
                    Vector3 raycastTarget = Vector3.Zero;
                    float raycastDistance;

                    if (_controller.MoveIndicator.Y == 1 && (Target.IsEmpty() || ManualOverride == true))
                    {
                        raycastTarget = _referenceMatrix.Forward * _maxTargetDistance + _referenceMatrix.Translation;
                    }
                    else if (!Target.IsEmpty())
                    {
                        Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (RaycastDistanceGrowthSpeed * (float)timeSinceLastTargetDetection.TotalSeconds);
                        raycastTarget = estimatedTargetPos + raycastOvershoot;
                    }

                    raycastDistance = (raycastTarget - cameraPos).Length();
                    raycastTarget = raycastDistance > MaxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * MaxRaycastDistance + cameraPos : raycastTarget;

                    if (_cameraArray[_raycastCounter].CanScan(raycastTarget))
                    {
                        MyDetectedEntityInfo raycastResult = _cameraArray[_raycastCounter].Raycast(raycastTarget);
                        _totalAvailRaycastDistance -= raycastDistance;
                        _raycastCounter++;
                        _raycastCounter %= _cameraArray.Count;

                        if (!raycastResult.IsEmpty())
                        {
                            _detectedEntity = raycastResult;

                            if (!Target.IsEmpty() && _detectedEntity.EntityId == Target.EntityID)
                            {
                                Target = new TargetInfo(_detectedEntity.EntityId, _detectedEntity.Position, _detectedEntity.Velocity, time);
                            }

                            else if (Target.IsEmpty())
                            {
                                if (_detectedEntity.EntityId == _previouslyDetectedEntity.EntityId)
                                {
                                    _matchingDetectionCounter += 1;
                                }
                                else
                                {
                                    _lastUniqueDetectionTime = time;
                                    _matchingDetectionCounter = 0;
                                }

                                _previouslyDetectedEntity = _detectedEntity;

                                TimeSpan timeSinceLastUniqueDetection = time - _lastUniqueDetectionTime;
                                if (timeSinceLastUniqueDetection.TotalSeconds > 2 && _matchingDetectionCounter >= 3)
                                {
                                    Target = new TargetInfo(_detectedEntity.EntityId, _detectedEntity.Position, _detectedEntity.Velocity, time);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
