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

            private IReadOnlyDictionary<long, EntityInfoExt> _targetInfo = new Dictionary<long, EntityInfoExt>();
            private HashSet<long> _targetsToRemove = new HashSet<long>();
            private PriorityQueue<long, double> _targetQueue;
            private float _maxRaycastDistance;
            private double _time;
            public float MaxRaycastDistance
            {
                get
                {
                    return _maxRaycastDistance;
                }
                set
                {
                    _maxRaycastDistance = value;
                    _cameraArray0.MaxRaycastDistance = value * 1.1f;
                    _cameraArray1.MaxRaycastDistance = value * 1.1f;
                    _cameraArray2.MaxRaycastDistance = value * 1.1f;
                    _cameraArray3.MaxRaycastDistance = value * 1.1f;
                }
            }
            public int TargetCount => _targetQueue.Count;

            public event Action<EntityInfoExt> OnTargetUpdated;
            public AWACS(IReadOnlyDictionary<long, EntityInfoExt> targetInfo)
            {
                _targetInfo = targetInfo;
                Init();
            }

            private void Init()
            {
                _spinRotor = new Rotor($"AWACS SPIN ROTOR");

                _maxRaycastDistance = Config.Get("AWACS", "MaxDistance").ToSingle(5000);
                Config.Set("AWACS", "MaxDistance", _maxRaycastDistance);

                MePb.CustomData = Config.ToString();

                Func<long, double> prioritySelector = targetID => _targetInfo[targetID].TimeRecorded;
                _targetQueue = new PriorityQueue<long, double>(prioritySelector);

                _cameraArray0 = new CameraArray("AWACS 0", _maxRaycastDistance * 1.1f);
                _cameraArray1 = new CameraArray("AWACS 1", _maxRaycastDistance * 1.1f);
                _cameraArray2 = new CameraArray("AWACS 2", _maxRaycastDistance * 1.1f);
                _cameraArray3 = new CameraArray("AWACS 3", _maxRaycastDistance * 1.1f);
            }

            public void Run(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }
                double globalTime = SystemCoordinator.GlobalTime;

                _cameraArray0.Update(time);
                _cameraArray1.Update(time);
                _cameraArray2.Update(time);
                _cameraArray3.Update(time);

                if (_targetQueue.Count != 0)
                {
                    Quaternion rotation = Quaternion.CreateFromAxisAngle(_spinRotor.RotorBlock.WorldMatrix.Up, _spinRotor.AngleRad);

                    _referenceMatrix = MatrixD.Transform(_spinRotor.RotorBlock.WorldMatrix, rotation);

                    _referenceMatrix.Translation = _spinRotor.RotorBlock.GetPosition();

                    for (int i = 0; i < _targetQueue.Count; i++)
                    {
                        if (_cameraArray0.Recharging && _cameraArray1.Recharging && _cameraArray2.Recharging && _cameraArray3.Recharging)
                        {
                            break;
                        }

                        long targetID = _targetQueue.Dequeue();
                        if (!_targetInfo.ContainsKey(targetID) || _targetsToRemove.Contains(targetID))
                        {
                            _targetsToRemove.Remove(targetID);
                            continue;
                        }
                        EntityInfoExt target = _targetInfo[targetID];
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
                            EntityInfoExt updatedTarget = new EntityInfoExt(raycastResult, globalTime);
                            OnTargetUpdated?.Invoke(updatedTarget);
                        }
                        _targetQueue.Enqueue(targetID);
                    }
                }
                _time = time;
            }

            public void AddTarget(long targetID)
            {
                if (!_targetInfo.ContainsKey(targetID))
                {
                    return;
                }
                _targetsToRemove.Remove(targetID);
                _targetQueue.Enqueue(targetID);
            }

            public void RemoveTarget(long targetID)
            {
                _targetsToRemove.Remove(targetID);
            }

            public void AppendOverview(StringBuilder sb)
            {
                sb.AppendLine("[AWACS]");
                sb.Append("  TRGTS: ").AppendFormat("{0:F0}", TargetCount).AppendLine();
                sb.Append("  RNG: ").AppendFormat("{0:F0}", _maxRaycastDistance).Append(" m");
            }
        }
    }
}
