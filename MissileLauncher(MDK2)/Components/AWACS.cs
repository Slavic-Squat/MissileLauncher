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
            private Rotor _spinRotor;
            private CameraArray _cameraArray0;
            private CameraArray _cameraArray1;
            private CameraArray _cameraArray2;
            private CameraArray _cameraArray3;

            private MatrixD _referenceMatrix;
            
            private Dictionary<long, EntityInfoExt> _targets = new Dictionary<long, EntityInfoExt>();
            private PriorityQueue<long, double> _targetQueue;
            private float _maxRaycastDistance;

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
            public IReadOnlyDictionary<long, EntityInfoExt> Targets => _targets;
            public int TargetCount => _targets.Count;
            public AWACS(float maxRaycastDistance = 5000)
            {
                _maxRaycastDistance = maxRaycastDistance;

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                _spinRotor = new Rotor($"AWACS SPIN ROTOR");
            }

            private void Init()
            {
                Func<long, double> prioritySelector = targetID => _targets[targetID].TimeRecorded;
                _targetQueue = new PriorityQueue<long, double>(prioritySelector);

                _cameraArray0 = new CameraArray("AWACS CAMERA ARRAY 0", _maxRaycastDistance);
                _cameraArray1 = new CameraArray("AWACS CAMERA ARRAY 1", _maxRaycastDistance);
                _cameraArray2 = new CameraArray("AWACS CAMERA ARRAY 2", _maxRaycastDistance);
                _cameraArray3 = new CameraArray("AWACS CAMERA ARRAY 3", _maxRaycastDistance);
            }

            public void Run(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }
                double globalTime = SystemCoordinator.GlobalTime;

                _cameraArray0.Update(time);
                _cameraArray1.Update(time);
                _cameraArray2.Update(time);
                _cameraArray3.Update(time);

                if (_targets.Count != 0)
                {
                    Quaternion rotation = Quaternion.CreateFromAxisAngle(_spinRotor.RotorBlock.WorldMatrix.Up, _spinRotor.CurrentAngle);

                    _referenceMatrix = MatrixD.Transform(_spinRotor.RotorBlock.WorldMatrix, rotation);

                    _referenceMatrix.Translation = _spinRotor.RotorBlock.GetPosition();

                    foreach (var targetID in _targets.Keys.ToList())
                    {
                        EntityInfoExt target = _targets[targetID];
                        double timeSinceLastDetection = globalTime - target.TimeRecorded;
                        Vector3D estimatedTargetPos = target.Position + target.Velocity * timeSinceLastDetection;
                        Vector3D estimatedTargetPosLocal = Vector3D.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, MatrixD.Transpose(_referenceMatrix));
                        double estimatedTargetDistance = estimatedTargetPosLocal.Length();
                        Vector3D estimatedTargetDirLocal = estimatedTargetDistance == 0 ? Vector3D.Zero : estimatedTargetPosLocal / estimatedTargetDistance;
                        double targetElevation = MathHelper.ToDegrees(Math.Asin(estimatedTargetDirLocal.Y));

                        if (timeSinceLastDetection > 5 || estimatedTargetDistance >= MaxRaycastDistance * 0.8 || targetElevation >= 45)
                        {
                            RemoveTarget(targetID);
                        }
                    }

                    for (int i = 0; i < _targetQueue.Count; i++)
                    {
                        if (_cameraArray0.Recharging && _cameraArray1.Recharging && _cameraArray2.Recharging && _cameraArray3.Recharging)
                        {
                            break;
                        }

                        long targetID = _targetQueue.Dequeue();
                        if (!_targets.ContainsKey(targetID))
                        {
                            continue;
                        }
                        EntityInfoExt target = _targets[targetID];
                        MyDetectedEntityInfo raycastResult = default(MyDetectedEntityInfo);
                        double timeSinceLastDetection = globalTime - target.TimeRecorded;
                        Vector3D estimatedTargetPos = target.Position + target.Velocity * timeSinceLastDetection;
                        Vector3D estimatedTargetPosLocal = Vector3D.TransformNormal(estimatedTargetPos - _referenceMatrix.Translation, MatrixD.Transpose(_referenceMatrix));
                        Vector3D estimatedTargetDirLocal = estimatedTargetPosLocal == Vector3D.Zero ? Vector3D.Zero : Vector3D.Normalize(estimatedTargetPosLocal);
                        double targetAzimuth = MathHelper.ToDegrees(Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z));

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
                            _targets[targetID] = new EntityInfoExt(raycastResult, globalTime);
                        }
                        _targetQueue.Enqueue(targetID);
                    }
                }
                Time = time;
            }

            public void AddTarget(EntityInfoExt target)
            {
                if (!target.IsValid)
                {
                    return;
                }
                if (!_targets.ContainsKey(target.EntityID))
                {
                    EntityInfo temp0 = new EntityInfo(target.EntityID, target.Position, target.Velocity, target.TimeRecorded);
                    EntityInfoExt temp1 = new EntityInfoExt(temp0, EntitySource.None, EntityRelation.Neutral, target.EntityID);

                    _targets.Add(target.EntityID, temp1);
                    _targetQueue.Enqueue(target.EntityID);
                }
                else
                {
                    var original = _targets[target.EntityID];
                    _targets[target.EntityID] = original.MergeKinematics(target);
                }
            }

            public void RemoveTarget(long targetID)
            {
                _targets.Remove(targetID);
            }
        }
    }
}
