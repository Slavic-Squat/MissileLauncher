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
            private Rotor _spinRotor;
            private CameraArray _cameraArray0;
            private CameraArray _cameraArray1;
            private CameraArray _cameraArray2;
            private CameraArray _cameraArray3;
            #endregion

            #region State Info
            private Matrix _referenceMatrix;

            private double _lastRunTime;
            #endregion

            #region Properties
            public int ID { get; private set; }
            public double Time { get; private set; }
            public float MaxRaycastDistance
            {
                get
                {
                    return _maxRaycastDistance;
                }
                set
                {
                    _maxRaycastDistance = value;
                    _cameraArray0.MaxRaycastDistance = value;
                    _cameraArray1.MaxRaycastDistance = value;
                    _cameraArray2.MaxRaycastDistance = value;
                    _cameraArray3.MaxRaycastDistance = value;
                }
            }
            public Dictionary<long, EntityInfoExt> Targets { get; private set; }
            public int TargetCount => Targets.Count;
            public Dictionary<long, bool> TargetsSyncInfo {  get; private set; }
            #endregion

            private float _maxRaycastDistance;
            public AWACS(int id, float maxRaycastDistance = 5000)
            {
                ID = id;
                _maxRaycastDistance = maxRaycastDistance;

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                _spinRotor = new Rotor($"AWACS {ID} Spin Rotor");
            }

            private void Init()
            {
                Targets = new Dictionary<long, EntityInfoExt>();
                TargetsSyncInfo = new Dictionary<long, bool>();

                _cameraArray0 = new CameraArray(1, _maxRaycastDistance);
                _cameraArray1 = new CameraArray(2, _maxRaycastDistance);
                _cameraArray2 = new CameraArray(3, _maxRaycastDistance);
                _cameraArray3 = new CameraArray(4, _maxRaycastDistance);
            }

            public void Run(double time)
            {
                Time = time;
                if (_lastRunTime == 0)
                    _lastRunTime = time;

                _cameraArray0.Update(time);
                _cameraArray1.Update(time);
                _cameraArray2.Update(time);
                _cameraArray3.Update(time);

                if (Targets.Count != 0)
                {
                    Quaternion rotation = Quaternion.CreateFromAxisAngle(_spinRotor.RotorBlock.WorldMatrix.Up, _spinRotor.CurrentAngle);

                    _referenceMatrix = Matrix.Transform(_spinRotor.RotorBlock.WorldMatrix, rotation);

                    _referenceMatrix.Translation = _spinRotor.RotorBlock.GetPosition();

                    foreach (var targetID in Targets.Keys.ToList())
                    {
                        float timeSinceLastDetection = (float)(time - Targets[targetID].TimeRecorded);
                        Vector3 estimatedTargetPos = Targets[targetID].Position + Targets[targetID].Velocity * timeSinceLastDetection;
                        Vector3 estimatedTargetPosLocal = Vector3.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix));
                        float estimatedTargetDistance = estimatedTargetPosLocal.Length();
                        Vector3 estimatedTargetDirLocal = estimatedTargetDistance == 0 ? Vector3.Zero : estimatedTargetPosLocal / estimatedTargetDistance;
                        float targetElevation = MathHelper.ToDegrees((float)Math.Asin(estimatedTargetDirLocal.Y));

                        if (timeSinceLastDetection > 5 || estimatedTargetDistance >= MaxRaycastDistance * 0.8f || targetElevation >= 45)
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
                        float timeSinceLastDetection = (float)(time - Targets[targetID].TimeRecorded);
                        Vector3 estimatedTargetPos = Targets[targetID].Position + Targets[targetID].Velocity * timeSinceLastDetection;
                        Vector3 estimatedTargetPosLocal = Vector3.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, Matrix.Transpose(_referenceMatrix));
                        Vector3 estimatedTargetDirLocal = estimatedTargetPosLocal == Vector3.Zero ? Vector3.Zero : Vector3.Normalize(estimatedTargetPosLocal);
                        float targetAzimuth = MathHelper.ToDegrees((float)Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z));

                        if (targetAzimuth > -45 && targetAzimuth < 45)
                        {
                            raycastResult = _cameraArray0.Raycast(estimatedTargetPos, 0.1f);
                        }
                        else if (targetAzimuth > 45 && targetAzimuth < 135)
                        {
                            raycastResult = _cameraArray1.Raycast(estimatedTargetPos, 0.1f);
                        }
                        else if (targetAzimuth > 135 || targetAzimuth < -135)
                        {
                            raycastResult = _cameraArray2.Raycast(estimatedTargetPos, 0.1f);
                        }
                        else if (targetAzimuth > -135 && targetAzimuth < -45)
                        {
                            raycastResult = _cameraArray3.Raycast(estimatedTargetPos, 0.1f);
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
