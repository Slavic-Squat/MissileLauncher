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
            private CameraArray _cameraArray0;
            private CameraArray _cameraArray1;
            private CameraArray _cameraArray2;
            private CameraArray _cameraArray3;
            #endregion

            #region State Info
            private Matrix _referenceMatrix;

            private float _spinRotorAngle;
            private bool _spinRotorInverted;

            private DateTime _lastRunTime;
            #endregion

            #region Properties
            public int ID { get; private set; }
            public float MaxRaycastDistance
            {
                get
                {
                    return _cameraArray0.MaxRaycastDistance;
                }
                set
                {
                    _cameraArray0.MaxRaycastDistance = value;
                    _cameraArray1.MaxRaycastDistance = value;
                    _cameraArray2.MaxRaycastDistance = value;
                    _cameraArray3.MaxRaycastDistance = value;
                }
            }
            public float RaycastDistanceGrowthSpeed { get; set; }
            public Dictionary<long, EntityInfoExt> Targets { get; private set; }
            public Dictionary<long, bool> TargetsSyncInfo {  get; private set; }
            #endregion
            public AWACS(int id, float maxRaycastDistance = 5000)
            {
                ID = id;

                Targets = new Dictionary<long, EntityInfoExt>();
                TargetsSyncInfo = new Dictionary<long, bool>();

                _cameraArray0 = new CameraArray(1, maxRaycastDistance);
                _cameraArray1 = new CameraArray(2, maxRaycastDistance);
                _cameraArray2 = new CameraArray(3, maxRaycastDistance);
                _cameraArray3 = new CameraArray(4, maxRaycastDistance);

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                _spinRotor = GTS.GetBlockWithName($"Spin Rotor [{ID}]") as IMyMotorStator;
                if (_spinRotor == null)
                {
                    throw new Exception("No Spin Rotor Found For AWACS");
                }
            }

            private void Init()
            {
                _spinRotorInverted = _spinRotor.CustomData.Contains("Inverted");
            }

            public void Run(DateTime time)
            {
                if (_lastRunTime == default(DateTime))
                    _lastRunTime = time;

                _cameraArray0.Update(time);
                _cameraArray1.Update(time);
                _cameraArray2.Update(time);
                _cameraArray3.Update(time);

                if (Targets.Count != 0)
                {
                    _spinRotorAngle = _spinRotorInverted ? -_spinRotor.Angle : _spinRotor.Angle;
                    _spinRotorAngle = MiscUtilities.LoopInRange(_spinRotorAngle, -(float)Math.PI, (float)Math.PI);

                    Quaternion rotation = Quaternion.CreateFromAxisAngle(_spinRotor.WorldMatrix.Up, _spinRotorAngle);

                    _referenceMatrix = Matrix.Transform(_spinRotor.WorldMatrix, rotation);

                    _referenceMatrix.Translation = _spinRotor.GetPosition();

                    foreach (var targetID in Targets.Keys.ToList())
                    {
                        TimeSpan timeSinceLastDetection = time - Targets[targetID].TimeRecorded;
                        Vector3 estimatedTargetPos = Targets[targetID].Position + Targets[targetID].Velocity * (float)timeSinceLastDetection.TotalSeconds;
                        Vector3 estimatedTargetPosLocal = Vector3.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix));
                        Vector3 estimatedTargetDirLocal = estimatedTargetPosLocal == Vector3.Zero ? Vector3.Zero : Vector3.Normalize(estimatedTargetPosLocal);
                        float estimatedTargetDistance = (estimatedTargetPos - _referenceMatrix.Translation).Length();
                        float targetElevation = MathHelper.ToDegrees((float)Math.Asin(estimatedTargetDirLocal.Y));

                        if (timeSinceLastDetection.TotalSeconds > 5 || estimatedTargetDistance >= MaxRaycastDistance * 0.8f || targetElevation >= 45)
                        {
                            RemoveTarget(targetID);
                        }
                    }

                    var OrderedTargetIDs = Targets.Keys.OrderBy(id => Targets[id].TimeRecorded);

                    foreach (long targetID in OrderedTargetIDs)
                    {
                        if (_cameraArray0.Recharging && _cameraArray1.Recharging && _cameraArray2.Recharging && _cameraArray3.Recharging)
                        {
                            break;
                        }

                        EntityInfoExt target = Targets[targetID];
                        MyDetectedEntityInfo raycastResult = default(MyDetectedEntityInfo);
                        TimeSpan timeSinceLastDetection = time - Targets[targetID].TimeRecorded;
                        Vector3 estimatedTargetPos = Targets[targetID].Position + Targets[targetID].Velocity * (float)timeSinceLastDetection.TotalSeconds;
                        Vector3 estimatedTargetPosLocal = Vector3.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix));
                        Vector3 estimatedTargetDirLocal = estimatedTargetPosLocal == Vector3.Zero ? Vector3.Zero : Vector3.Normalize(estimatedTargetPosLocal);
                        float targetAzimuth = MathHelper.ToDegrees((float)Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z));

                        if (targetAzimuth > -45 && targetAzimuth < 45)
                        {
                            raycastResult = _cameraArray0.Raycast(estimatedTargetPos, time, 0.1f);
                        }
                        else if (targetAzimuth > 45 && targetAzimuth < 135)
                        {
                            raycastResult = _cameraArray1.Raycast(estimatedTargetPos, time, 0.1f);
                        }
                        else if (targetAzimuth > 135 || targetAzimuth < -135)
                        {
                            raycastResult = _cameraArray2.Raycast(estimatedTargetPos, time, 0.1f);
                        }
                        else if (targetAzimuth > -135 && targetAzimuth < -45)
                        {
                            raycastResult = _cameraArray3.Raycast(estimatedTargetPos, time, 0.1f);
                        }

                        if (raycastResult.EntityId == targetID)
                        {
                            var freshTarget = new EntityInfoExt(raycastResult, time);
                            var originalTarget = Targets[targetID];
                            Targets[targetID] = originalTarget.Merge(freshTarget);
                            TargetsSyncInfo[targetID] = true;
                        }
                    }
                }
                _lastRunTime = time;
            }

            public void AddTarget(EntityInfoExt target)
            {
                Targets[target.EntityID] = target;
                TargetsSyncInfo[target.EntityID] = false;
            }

            public void RemoveTarget(long targetID)
            {
                Targets.Remove(targetID);
                TargetsSyncInfo.Remove(targetID);
            }
        }
    }
}
