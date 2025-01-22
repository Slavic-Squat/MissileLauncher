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
        public class AWACS
        {
            #region Parts
            private IMyMotorStator _spinRotor;
            private List<IMyCameraBlock> _cameraArray0 = new List<IMyCameraBlock>();
            private List<IMyCameraBlock> _cameraArray1 = new List<IMyCameraBlock>();
            private List<IMyCameraBlock> _cameraArray2 = new List<IMyCameraBlock>();
            private List<IMyCameraBlock> _cameraArray3 = new List<IMyCameraBlock>();
            #endregion

            #region State Info
            private Matrix _referenceMatrix;

            private float _maxTargetDistance;

            private float _spinRotorAngle;
            private bool _spinRotorInverted;

            private int _raycastCounter0;
            private int _raycastCounter1;
            private int _raycastCounter2;
            private int _raycastCounter3;

            private float _totalAvailRaycastDistance;

            private int _targetIndex;
            #endregion

            #region Properties
            public Program Program { get; private set; }
            public int ID { get; private set; }
            public float MaxRaycastDistance { get; set; }
            public float RaycastDistanceGrowthSpeed { get; set; }
            public Dictionary<long, TargetInfo> Targets { get; private set; }
            public Dictionary<long, bool> TargetsSyncInfo {  get; private set; }
            public List<long> TargetIDs { get; private set; }
            #endregion

            public AWACS(Program program, int id, float maxRaycastDistance = 5000, float raycastDistanceGrowthSpeed = 200)
            {
                Program = program;
                ID = id;
                MaxRaycastDistance = maxRaycastDistance;
                _maxTargetDistance = maxRaycastDistance * 0.8f;
                RaycastDistanceGrowthSpeed = raycastDistanceGrowthSpeed;

                Targets = new Dictionary<long, TargetInfo>();
                TargetsSyncInfo = new Dictionary<long, bool>();
                TargetIDs = new List<long>();

                TryGetBlocks();
                Init();
            }

            public bool TryGetBlocks()
            {
                try
                {
                    _spinRotor = Program.GridTerminalSystem.GetBlockWithName($"Spin Rotor [{ID}]") as IMyMotorStator;
                    if (_spinRotor == null)
                    {
                        throw new Exception();
                    }
                    var cameraGroup0 = Program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array0 [{ID}]");
                    if (cameraGroup0 == null)
                    {
                        throw new Exception();
                    }
                    cameraGroup0.GetBlocksOfType<IMyCameraBlock>(_cameraArray0);
                    var cameraGroup1 = Program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array1 [{ID}]");
                    if (cameraGroup1 == null)
                    {
                        throw new Exception();
                    }
                    cameraGroup1.GetBlocksOfType<IMyCameraBlock>(_cameraArray1);
                    var cameraGroup2 = Program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array2 [{ID}]");
                    if (cameraGroup2 == null)
                    {
                        throw new Exception();
                    }
                    cameraGroup2.GetBlocksOfType<IMyCameraBlock>(_cameraArray2);
                    var cameraGroup3 = Program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array3 [{ID}]");
                    if (cameraGroup3 == null)
                    {
                        throw new Exception();
                    }
                    cameraGroup3.GetBlocksOfType<IMyCameraBlock>(_cameraArray3);
                    return true;
                }
                catch (Exception ex)
                {
                    Program.Echo("Error in AWACS construction");
                    return false;
                }
            }

            public void Init()
            {
                foreach (IMyCameraBlock camera in _cameraArray0)
                {
                    camera.EnableRaycast = true;
                    _totalAvailRaycastDistance += (float)camera.AvailableScanRange;
                }

                foreach (IMyCameraBlock camera in _cameraArray1)
                {
                    camera.EnableRaycast = true;
                    _totalAvailRaycastDistance += (float)camera.AvailableScanRange;
                }

                foreach (IMyCameraBlock camera in _cameraArray2)
                {
                    camera.EnableRaycast = true;
                    _totalAvailRaycastDistance += (float)camera.AvailableScanRange;
                }

                foreach (IMyCameraBlock camera in _cameraArray3)
                {
                    camera.EnableRaycast = true;
                    _totalAvailRaycastDistance += (float)camera.AvailableScanRange;
                }

                _spinRotorInverted = _spinRotor.CustomData.Contains("Inverted");
            }

            public void Run(DateTime time)
            {
                float timeDeltaMiliseconds = (float)Program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                _totalAvailRaycastDistance += 2 * timeDeltaMiliseconds * (_cameraArray0.Count + _cameraArray1.Count + _cameraArray2.Count + _cameraArray3.Count);

                if (TargetIDs.Count != 0)
                {
                    _spinRotorAngle = _spinRotorInverted ? -_spinRotor.Angle : _spinRotor.Angle;
                    _spinRotorAngle = MiscUtilities.LoopInRange(_spinRotorAngle, -(float)Math.PI, (float)Math.PI);

                    Quaternion rotation = Quaternion.CreateFromAxisAngle(_spinRotor.WorldMatrix.Up, _spinRotorAngle);

                    _referenceMatrix = Matrix.Transform(_spinRotor.WorldMatrix, rotation);

                    _referenceMatrix.Translation = _spinRotor.GetPosition();

                    for (int i = TargetIDs.Count - 1; i >= 0; i--)
                    {
                        long targetID = TargetIDs[i];
                        TimeSpan timeSinceLastDetection = time - Targets[targetID].TimeRecorded;
                        Vector3 estimatedTargetPos = Targets[targetID].Position + Targets[targetID].Velocity * (float)timeSinceLastDetection.TotalSeconds;
                        Vector3 estimatedTargetDirLocal = Vector3.Normalize(Vector3.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix)));
                        float estimatedTargetDistance = (estimatedTargetPos - _referenceMatrix.Translation).Length();
                        float targetElevationLocal = MathHelper.ToDegrees((float)Math.Asin(estimatedTargetDirLocal.Y));

                        if (timeSinceLastDetection.TotalSeconds > 5 || estimatedTargetDistance >= _maxTargetDistance || targetElevationLocal >= 45)
                        {
                            RemoveTarget(targetID);
                        }
                    }

                    float baseAvailRaycastDistance = 2 * MaxRaycastDistance * (_cameraArray0.Count + _cameraArray1.Count + _cameraArray2.Count + _cameraArray3.Count);

                    if (TargetIDs.Count != 0)
                    {
                        _targetIndex %= TargetIDs.Count;
                    }
                    for (; (_targetIndex < TargetIDs.Count) && (_totalAvailRaycastDistance >= baseAvailRaycastDistance); _targetIndex++)
                    {
                        long targetID = TargetIDs[_targetIndex];
                        MyDetectedEntityInfo raycastResult;
                        TimeSpan timeSinceLastDetection = time - Targets[targetID].TimeRecorded;
                        Vector3 estimatedTargetPos = Targets[targetID].Position + Targets[targetID].Velocity * (float)timeSinceLastDetection.TotalSeconds;
                        Vector3 estimatedTargetDirLocal = Vector3.Normalize(Vector3.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix)));
                        float targetAzimuthLocal = MathHelper.ToDegrees((float)Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z));

                        if (targetAzimuthLocal > -45 && targetAzimuthLocal < 45)
                        {
                            Vector3 cameraPos = _cameraArray0[_raycastCounter0].GetPosition();
                            Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (RaycastDistanceGrowthSpeed * (float)timeSinceLastDetection.TotalSeconds);
                            Vector3 raycastTarget = estimatedTargetPos + raycastOvershoot;
                            float raycastDistance = (raycastTarget - cameraPos).Length();
                            raycastTarget = raycastDistance > MaxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * MaxRaycastDistance + cameraPos : raycastTarget;

                            if (_cameraArray0[_raycastCounter0].CanScan(raycastTarget))
                            {
                                raycastResult = _cameraArray0[_raycastCounter0].Raycast(raycastTarget);
                                _totalAvailRaycastDistance -= raycastDistance;
                                _raycastCounter0++;
                                _raycastCounter0 %= _cameraArray0.Count;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (targetAzimuthLocal > 45 && targetAzimuthLocal < 135)
                        {
                            Vector3 cameraPos = _cameraArray1[_raycastCounter1].GetPosition();
                            Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (RaycastDistanceGrowthSpeed * (float)timeSinceLastDetection.TotalSeconds);
                            Vector3 raycastTarget = estimatedTargetPos + raycastOvershoot;
                            float raycastDistance = (raycastTarget - cameraPos).Length();
                            raycastTarget = raycastDistance > MaxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * MaxRaycastDistance + cameraPos : raycastTarget;

                            if (_cameraArray1[_raycastCounter1].CanScan(raycastTarget))
                            {
                                raycastResult = _cameraArray1[_raycastCounter1].Raycast(raycastTarget);
                                _totalAvailRaycastDistance -= raycastDistance;
                                _raycastCounter1++;
                                _raycastCounter1 %= _cameraArray1.Count;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (targetAzimuthLocal > 135 || targetAzimuthLocal < -135)
                        {
                            Vector3 cameraPos = _cameraArray2[_raycastCounter2].GetPosition();
                            Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (RaycastDistanceGrowthSpeed * (float)timeSinceLastDetection.TotalSeconds);
                            Vector3 raycastTarget = estimatedTargetPos + raycastOvershoot;
                            float raycastDistance = (raycastTarget - cameraPos).Length();
                            raycastTarget = raycastDistance > MaxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * MaxRaycastDistance + cameraPos : raycastTarget;

                            if (_cameraArray2[_raycastCounter2].CanScan(raycastTarget))
                            {
                                raycastResult = _cameraArray2[_raycastCounter2].Raycast(raycastTarget);
                                _totalAvailRaycastDistance -= raycastDistance;
                                _raycastCounter2++;
                                _raycastCounter2 %= _cameraArray2.Count;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (targetAzimuthLocal > -135 && targetAzimuthLocal < -45)
                        {
                            Vector3 cameraPos = _cameraArray3[_raycastCounter3].GetPosition();
                            Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (RaycastDistanceGrowthSpeed * (float)timeSinceLastDetection.TotalSeconds);
                            Vector3 raycastTarget = estimatedTargetPos + raycastOvershoot;
                            float raycastDistance = (raycastTarget - cameraPos).Length();
                            raycastTarget = raycastDistance > MaxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * MaxRaycastDistance + cameraPos : raycastTarget;

                            if (_cameraArray3[_raycastCounter3].CanScan(raycastTarget))
                            {
                                raycastResult = _cameraArray3[_raycastCounter3].Raycast(raycastTarget);
                                _totalAvailRaycastDistance -= raycastDistance;
                                _raycastCounter3++;
                                _raycastCounter3 %= _cameraArray3.Count;

                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }

                        if (raycastResult.EntityId == targetID)
                        {
                            Targets[targetID] = new TargetInfo(raycastResult.EntityId, raycastResult.Position, raycastResult.Velocity, time);
                            TargetsSyncInfo[targetID] = true;
                        }
                    }
                }
            }

            public void AddTarget(TargetInfo target)
            {
                if (!TargetIDs.Contains(target.EntityID))
                {
                    TargetIDs.Add(target.EntityID);
                }

                Targets[target.EntityID] = target;
                TargetsSyncInfo[target.EntityID] = false;
            }

            public void RemoveTarget(long targetID)
            {
                int removedIndex = TargetIDs.IndexOf(targetID);
                if (_targetIndex > removedIndex)
                {
                    _targetIndex--;
                }
                TargetIDs.Remove(targetID);
                if (_targetIndex >= TargetIDs.Count)
                {
                    _targetIndex = 0;
                }
                Targets.Remove(targetID);
                TargetsSyncInfo.Remove(targetID);
            }
        }
    }
}
