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
            private IMyMotorStator _azimuthRotor;
            private IMyMotorStator _elevationRotor;
            private List<IMyCameraBlock> _cameraArray = new List<IMyCameraBlock>();
            #endregion

            #region State Info
            private DateTime _time;
            private DateTime _lastRunTime;
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
            private TimeSpan _timeSinceLastDetection;
            private int _matchingDetectionCounter;
            private MyDetectedEntityInfo _previouslyDetectedEntity;
            private Vector3 _estimatedTargetPosition;
            private float _estimatedTargetDistance;
            #endregion

            #region Controllers
            private PIDControl _azimuthPID;
            private PIDControl _elevationPID;
            #endregion

            private Action LoopedUserAction;
            private Action SingleUserAction;

            #region Properties
            public Program Program { get; private set; }
            public int ID { get; private set; }
            public float MaxRaycastDistance { get; set; }
            public float RaycastDistanceGrowthSpeed { get; set; }
            public float Sensitivity { get; set; }
            public bool ManualOverride { get; set; }
            public EntityInfo Target {  get; private set; }
            #endregion

            public TargetingLaser(Program program, int id, float sensitivity = 0.05f, float maxRaycastDistance = 5000, float raycastDistanceGrowthSpeed = 200, bool manualOverride = false)
            {
                Program = program;
                ID = id;
                Sensitivity = sensitivity;
                MaxRaycastDistance = maxRaycastDistance;
                _maxTargetDistance = MaxRaycastDistance * 0.8f;
                RaycastDistanceGrowthSpeed = raycastDistanceGrowthSpeed;

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
                if (_time == DateTime.MinValue)
                {
                    _time = time;
                }
                _lastRunTime = _time;
                _time = time;

                MoveLaser(0, 0);

                LoopedUserAction?.Invoke();
                SingleUserAction?.Invoke();
                SingleUserAction = null;

                if (Target != null && ManualOverride == false)
                {
                    AutoTrack();
                }
            }

            public void AutoTrack()
            {
                float timeDeltaMiliseconds = (float)(_time - _lastRunTime).TotalMilliseconds;
                float timeDeltaSeconds = (float)(_time - _lastRunTime).TotalSeconds;

                _totalAvailRaycastDistance += 2 * timeDeltaMiliseconds * _cameraArray.Count;

                _azimuthRotorAngle = _azimuthRotorInverted ? -_azimuthRotor.Angle : _azimuthRotor.Angle;
                _elevationRotorAngle = _elevationRotorInverted ? -_elevationRotor.Angle : _elevationRotor.Angle;

                Matrix H0 = _azimuthRotor.WorldMatrix;
                Matrix H1 = Matrix.CreateRotationY(_azimuthRotorAngle);
                Matrix H2 = Matrix.CreateRotationX(_elevationRotorAngle);
                H2.Translation = new Vector3(0, 3, 0);

                _referenceMatrix = H2 * H1 * H0;

                _timeSinceLastDetection = _time - Target.TimeRecorded;
                _estimatedTargetPosition = Target.Position + Target.Velocity * (float)_timeSinceLastDetection.TotalSeconds;
                _estimatedTargetDistance = (_estimatedTargetPosition - _referenceMatrix.Translation).Length();

                if (_estimatedTargetDistance > _maxTargetDistance || _timeSinceLastDetection.TotalSeconds > 5)
                {
                    ForgetTarget();
                }

                if (Target != null)
                {
                    Vector3 estimatedTargetDirLocal = Vector3.Normalize(Vector3.TransformNormal(_estimatedTargetPosition - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix)));
                    _azimuthError = (float)Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z);
                    _elevationError = (float)Math.Asin(estimatedTargetDirLocal.Y);
                    var azimuthInput = _azimuthPID.Run(_azimuthError, timeDeltaSeconds);
                    var elevationInput = _elevationPID.Run(_elevationError, timeDeltaSeconds);

                    MoveLaser(azimuthInput, elevationInput);
                }

                if (Target != null)
                {
                    FireLaser();
                }
            }

            public void UserMoveLaser(float azimuthInput, float elevtionInput)
            {
                if (Target == null || ManualOverride == true)
                {
                    MoveLaser(azimuthInput, elevtionInput);
                }
            }

            private void MoveLaser(float azimuthInput, float elevationInput)
            {
                _elevationRotor.TargetVelocityRad = _elevationRotorInverted ? -elevationInput * Sensitivity : elevationInput * Sensitivity;
                _azimuthRotor.TargetVelocityRad = _azimuthRotorInverted ? -azimuthInput * Sensitivity : azimuthInput * Sensitivity;
            }

            public void ForgetTarget()
            {
                Target = null;
                _matchingDetectionCounter = 0;
                _estimatedTargetPosition = Vector3.Zero;
                _estimatedTargetDistance = 0;
                _timeSinceLastDetection = TimeSpan.Zero;
            }

            public void UserFireLaser()
            {
                if (Target == null || ManualOverride == true)
                {
                    FireLaser();
                }
            }

            private void FireLaser()
            {
                float baseAvailRaycastDistance = 2 * MaxRaycastDistance * _cameraArray.Count;

                if (_totalAvailRaycastDistance >= baseAvailRaycastDistance)
                {
                    Vector3 cameraPos = _cameraArray[_raycastCounter].GetPosition();
                    Vector3 raycastTarget = Vector3.Zero;

                    if (Target == null || ManualOverride == true)
                    {
                        raycastTarget = _referenceMatrix.Forward * _maxTargetDistance + _referenceMatrix.Translation;
                    }
                    else if (Target != null)
                    {
                        Vector3 raycastOvershoot = Vector3.Normalize(_estimatedTargetPosition - cameraPos) * (RaycastDistanceGrowthSpeed * (float)_timeSinceLastDetection.TotalSeconds);
                        raycastTarget = _estimatedTargetPosition + raycastOvershoot;
                    }

                    float raycastDistance = (raycastTarget - cameraPos).Length();
                    raycastTarget = raycastDistance > MaxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * MaxRaycastDistance + cameraPos : raycastTarget;

                    if (_cameraArray[_raycastCounter].CanScan(raycastTarget))
                    {
                        MyDetectedEntityInfo raycastResult = _cameraArray[_raycastCounter].Raycast(raycastTarget);
                        _totalAvailRaycastDistance -= raycastDistance;
                        _raycastCounter++;
                        _raycastCounter %= _cameraArray.Count;

                        if (!raycastResult.IsEmpty())
                        {
                            if (Target != null && raycastResult.EntityId == Target.EntityID)
                            {
                                Target.UpdateFromRaycast(raycastResult, _time);
                            }

                            else if (Target == null)
                            {
                                if (raycastResult.EntityId == _previouslyDetectedEntity.EntityId)
                                {
                                    _matchingDetectionCounter += 1;
                                }
                                else
                                {
                                    _lastUniqueDetectionTime = _time;
                                    _matchingDetectionCounter = 0;
                                }

                                _previouslyDetectedEntity = raycastResult;

                                TimeSpan timeSinceLastUniqueDetection = _time - _lastUniqueDetectionTime;
                                if (timeSinceLastUniqueDetection.TotalSeconds > 2 && _matchingDetectionCounter >= 3)
                                {
                                    Target = EntityInfo.CreateFromRaycast(raycastResult, _time);
                                }
                            }
                        }
                    }
                }
            }

            public void SyncTarget(AWACS awacs)
            {
                if (awacs != null && Target != null)
                {
                    awacs.AddTarget(Target);
                }
            }

            public IEnumerator<bool> ControlLaser(ControlStation controlStation)
            {
                UserInput input = controlStation.UserInput;

                while (true)
                {
                    SingleUserAction += () => UserMoveLaser(input.MouseInput.X, input.MouseInput.Y);

                    if (input.SpacePress == true)
                    {
                        SingleUserAction += () => UserFireLaser();
                    }
                    if (input.CRelease == true)
                    {
                        SingleUserAction += () => ForgetTarget();
                    }
                    if (input.CHeld == true)
                    {
                        break;
                    }
                    yield return true;
                }
                yield return false;
            }
        }
    }
}
