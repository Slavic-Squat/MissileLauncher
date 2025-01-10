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
            #region General Info
            private Program program;
            private int ID;
            #endregion

            #region Parts
            private IMyMotorStator spinRotor;
            private List<IMyCameraBlock> cameraArray0 = new List<IMyCameraBlock>();
            private List<IMyCameraBlock> cameraArray1 = new List<IMyCameraBlock>();
            private List<IMyCameraBlock> cameraArray2 = new List<IMyCameraBlock>();
            private List<IMyCameraBlock> cameraArray3 = new List<IMyCameraBlock>();
            #endregion

            #region Properties
            private float spinRotorAngle;
            private bool spinRotorInverted;

            private float maxTargetDistance;
            private float maxRaycastDistance;
            private float raycastDistanceGrowthSpeed;

            private Matrix referenceMatrix;
            #endregion

            #region State Info
            private int raycastCounter0;
            private int raycastCounter1;
            private int raycastCounter2;
            private int raycastCounter3;

            private float totalAvailRaycastDistance;

            private int targetIndex;
            #endregion

            #region Output
            public Dictionary<long, MyTuple<Vector3, Vector3, DateTime>> lockedTargetsInfo = new Dictionary<long, MyTuple<Vector3, Vector3, DateTime>>();
            public Dictionary<long, bool> lockedTargetsSyncInfo = new Dictionary<long, bool>();
            public List<long> lockedTargetIDs = new List<long>();
            #endregion

            public AWACS(Program program, int ID, float maxRaycastDistance = 5000, float raycastDistanceGrowthSpeed = 200)
            {
                this.program = program;
                this.ID = ID;
                this.maxRaycastDistance = maxRaycastDistance;
                this.maxTargetDistance = maxRaycastDistance * 0.8f;
                this.raycastDistanceGrowthSpeed = raycastDistanceGrowthSpeed;
                
                try
                {
                    spinRotor = program.GridTerminalSystem.GetBlockWithName($"Spin Rotor [{ID}]") as IMyMotorStator;
                    if (spinRotor == null)
                    {
                        throw new Exception();
                    }
                    program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array0 [{ID}]").GetBlocksOfType<IMyCameraBlock>(cameraArray0);
                    program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array1 [{ID}]").GetBlocksOfType<IMyCameraBlock>(cameraArray1);
                    program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array2 [{ID}]").GetBlocksOfType<IMyCameraBlock>(cameraArray2);
                    program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array3 [{ID}]").GetBlocksOfType<IMyCameraBlock>(cameraArray3);
                }
                catch (Exception ex)
                {
                    program.Echo("Error in AWACS construction");
                    throw;
                }

                foreach (IMyCameraBlock camera in cameraArray0)
                {
                    camera.EnableRaycast = true;
                }

                foreach (IMyCameraBlock camera in cameraArray1)
                {
                    camera.EnableRaycast = true;
                }

                foreach (IMyCameraBlock camera in cameraArray2)
                {
                    camera.EnableRaycast = true;
                }

                foreach (IMyCameraBlock camera in cameraArray3)
                {
                    camera.EnableRaycast = true;
                }

                spinRotorInverted = spinRotor.CustomData.Contains("Inverted");
            }

            public void Run(DateTime time)
            {
                if (lockedTargetIDs.Count != 0)
                {
                    float timeDeltaMiliseconds = (float)program.Runtime.TimeSinceLastRun.TotalMilliseconds;

                    referenceMatrix = spinRotor.WorldMatrix;
                    spinRotorAngle = spinRotorInverted ? -spinRotor.Angle : spinRotor.Angle;
                    spinRotorAngle = Math.Abs(spinRotorAngle) > Math.PI ? (float)((2 * Math.PI - Math.Abs(spinRotorAngle)) * -Math.Sign(spinRotorAngle)) : spinRotorAngle;

                    Quaternion rotation = Quaternion.CreateFromAxisAngle(referenceMatrix.Up, spinRotorAngle);

                    Matrix.Transform(ref referenceMatrix, ref rotation, out referenceMatrix);

                    referenceMatrix.Translation = spinRotor.GetPosition();

                    for (int i = lockedTargetIDs.Count - 1; i >= 0; i--)
                    {
                        long targetID = lockedTargetIDs[i];
                        TimeSpan timeSinceLastDetection = time - lockedTargetsInfo[targetID].Item3;
                        Vector3 estimatedTargetPos = (lockedTargetsInfo[targetID].Item1 + lockedTargetsInfo[targetID].Item2 * (float)timeSinceLastDetection.TotalSeconds);
                        float estimatedTargetDistance = (estimatedTargetPos - referenceMatrix.Translation).Length();

                        if (timeSinceLastDetection.TotalSeconds > 50 || estimatedTargetDistance > maxTargetDistance)
                        {
                            RemoveTarget(targetID);
                        }
                    }

                    totalAvailRaycastDistance += 2 * timeDeltaMiliseconds * (cameraArray0.Count + cameraArray1.Count + cameraArray2.Count + cameraArray3.Count);
                    float baseAvailRaycastDistance = 2 * maxRaycastDistance * (cameraArray0.Count + cameraArray1.Count + cameraArray2.Count + cameraArray3.Count);

                    if (lockedTargetIDs.Count != 0)
                    {
                        targetIndex %= lockedTargetIDs.Count;
                    }
                    for (; (targetIndex < lockedTargetIDs.Count) && (totalAvailRaycastDistance >= baseAvailRaycastDistance); targetIndex++)
                    {
                        long targetID = lockedTargetIDs[targetIndex];
                        MyDetectedEntityInfo raycastResult;
                        TimeSpan timeSinceLastDetection = time - lockedTargetsInfo[targetID].Item3;
                        Vector3 estimatedTargetPos = (lockedTargetsInfo[targetID].Item1 + lockedTargetsInfo[targetID].Item2 * (float)timeSinceLastDetection.TotalSeconds);
                        Vector3 estimatedTargetDirLocal = Vector3.Normalize(Vector3.TransformNormal(estimatedTargetPos - referenceMatrix.Translation, Matrix.Transpose(referenceMatrix)));
                        float targetAzimuthLocal = (float)(Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z) * (180 / Math.PI));

                        if (targetAzimuthLocal > -45 && targetAzimuthLocal < 45)
                        {
                            Vector3 cameraPos = cameraArray0[raycastCounter0].GetPosition();
                            Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (raycastDistanceGrowthSpeed * (float)timeSinceLastDetection.TotalSeconds);
                            Vector3 raycastTarget = estimatedTargetPos + raycastOvershoot;
                            float raycastDistance = (raycastTarget - cameraPos).Length();
                            raycastTarget = raycastDistance > maxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * maxRaycastDistance + cameraPos : raycastTarget;

                            if (cameraArray0[raycastCounter0].CanScan(raycastTarget))
                            {
                                raycastResult = cameraArray0[raycastCounter0].Raycast(raycastTarget);
                                totalAvailRaycastDistance -= raycastDistance;
                                raycastCounter0++;
                                raycastCounter0 %= cameraArray0.Count;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (targetAzimuthLocal > 45 && targetAzimuthLocal < 135)
                        {
                            Vector3 cameraPos = cameraArray1[raycastCounter1].GetPosition();
                            Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (raycastDistanceGrowthSpeed * (float)timeSinceLastDetection.TotalSeconds);
                            Vector3 raycastTarget = estimatedTargetPos + raycastOvershoot;
                            float raycastDistance = (raycastTarget - cameraPos).Length();
                            raycastTarget = raycastDistance > maxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * maxRaycastDistance + cameraPos : raycastTarget;

                            if (cameraArray1[raycastCounter1].CanScan(raycastTarget))
                            {
                                raycastResult = cameraArray1[raycastCounter1].Raycast(raycastTarget);
                                totalAvailRaycastDistance -= raycastDistance;
                                raycastCounter1++;
                                raycastCounter1 %= cameraArray1.Count;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (targetAzimuthLocal > 135 || targetAzimuthLocal < -135)
                        {
                            Vector3 cameraPos = cameraArray2[raycastCounter2].GetPosition();
                            Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (raycastDistanceGrowthSpeed * (float)timeSinceLastDetection.TotalSeconds);
                            Vector3 raycastTarget = estimatedTargetPos + raycastOvershoot;
                            float raycastDistance = (raycastTarget - cameraPos).Length();
                            raycastTarget = raycastDistance > maxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * maxRaycastDistance + cameraPos : raycastTarget;

                            if (cameraArray2[raycastCounter2].CanScan(raycastTarget))
                            {
                                raycastResult = cameraArray2[raycastCounter2].Raycast(raycastTarget);
                                totalAvailRaycastDistance -= raycastDistance;
                                raycastCounter2++;
                                raycastCounter2 %= cameraArray2.Count;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (targetAzimuthLocal > -135 && targetAzimuthLocal < -45)
                        {
                            Vector3 cameraPos = cameraArray3[raycastCounter3].GetPosition();
                            Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (raycastDistanceGrowthSpeed * (float)timeSinceLastDetection.TotalSeconds);
                            Vector3 raycastTarget = estimatedTargetPos + raycastOvershoot;
                            float raycastDistance = (raycastTarget - cameraPos).Length();
                            raycastTarget = raycastDistance > maxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * maxRaycastDistance + cameraPos : raycastTarget;

                            if (cameraArray3[raycastCounter3].CanScan(raycastTarget))
                            {
                                raycastResult = cameraArray3[raycastCounter3].Raycast(raycastTarget);
                                totalAvailRaycastDistance -= raycastDistance;
                                raycastCounter3++;
                                raycastCounter3 %= cameraArray3.Count;

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
                            lockedTargetsInfo[targetID] = new MyTuple<Vector3, Vector3, DateTime>(raycastResult.Position, raycastResult.Velocity, time);
                            lockedTargetsSyncInfo[targetID] = true;
                        }
                    }
                }
            }

            public void AddTarget(long targetID, Vector3 position, Vector3 velocity, DateTime time)
            {
                if (!lockedTargetIDs.Contains(targetID))
                {
                    lockedTargetIDs.Add(targetID);
                }

                lockedTargetsInfo[targetID] = new MyTuple<Vector3, Vector3, DateTime>(position, velocity, time);
                lockedTargetsSyncInfo[targetID] = false;
            }

            public void RemoveTarget(long targetID)
            {
                int removedIndex = lockedTargetIDs.IndexOf(targetID);
                if (targetIndex > removedIndex)
                {
                    targetIndex--;
                }
                lockedTargetIDs.Remove(targetID);
                if (targetIndex >= lockedTargetIDs.Count)
                {
                    targetIndex = 0;
                }
                lockedTargetsInfo.Remove(targetID);
                lockedTargetsSyncInfo.Remove(targetID);
            }
        }
    }
}
