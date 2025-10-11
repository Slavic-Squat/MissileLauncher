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
        public class CameraArray
        {
            private List<IMyCameraBlock> _cameras = new List<IMyCameraBlock>();
            private DateTime _lastUpdateTime;
            private int _cameraIndex;
            private float _totalAvailableRaycastDistance;

            public int ID { get; private set; }
            public float MaxRaycastDistance { get; set; }
            public bool Recharging => _totalAvailableRaycastDistance < 2 * MaxRaycastDistance * _cameras.Count;
            public CameraArray(int id, float maxRaycastDistance)
            {
                ID = id;
                MaxRaycastDistance = maxRaycastDistance;

                GetBLocks();
                Init();
            }

            private void GetBLocks()
            {
                GTS.GetBlockGroupWithName($"Camera Array [{ID}]")?.GetBlocksOfType(_cameras);
                if (_cameras.Count == 0)
                {
                    throw new Exception($"Camera Array [{ID}] has no cameras!");
                }
            }

            private void Init()
            {
                foreach (var camera in _cameras)
                {
                    camera.EnableRaycast = true;
                }
            }

            public void Update(DateTime time)
            {
                if (_lastUpdateTime == default(DateTime))
                    _lastUpdateTime = time;

                _totalAvailableRaycastDistance += (float)(time - _lastUpdateTime).TotalSeconds * 2000f * _cameras.Count;
                _lastUpdateTime = time;
            }

            public MyDetectedEntityInfo Raycast(Vector3 raycastTarget, DateTime time)
            {
                if (CanScan(raycastTarget, time))
                {
                    var result = _cameras[_cameraIndex].Raycast(raycastTarget);
                    float distanceUsed = Vector3.Distance(raycastTarget, _cameras[_cameraIndex].GetPosition());
                    _totalAvailableRaycastDistance -= distanceUsed;
                    _cameraIndex = (_cameraIndex + 1) % _cameras.Count;

                    return result;
                }
                else
                {
                    return default(MyDetectedEntityInfo);
                }
            }

            public MyDetectedEntityInfo Raycast(Vector3 raycastTarget, DateTime time, float overshoot)
            {
                Vector3 raycastOvershoot = (raycastTarget - _cameras[_cameraIndex].GetPosition()) * overshoot;
                raycastTarget += raycastOvershoot;

                return Raycast(raycastTarget, time);
            }

            public bool CanScan(Vector3 raycastTarget, DateTime time)
            {
                Update(time);

                float raycastDistance = Vector3.Distance(raycastTarget, _cameras[_cameraIndex].GetPosition());

                if (_cameras[_cameraIndex].CanScan(raycastTarget) && !Recharging && raycastDistance < MaxRaycastDistance)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool CanScan(Vector3 raycastTarget, DateTime time, float overshoot)
            {
                Vector3 raycastOvershoot = (raycastTarget - _cameras[_cameraIndex].GetPosition()) * overshoot;
                raycastTarget += raycastOvershoot;

                return CanScan(raycastTarget, time);
            }

            public Vector3 GetCameraPosition() => _cameras[_cameraIndex].GetPosition();
        }
    }
}
