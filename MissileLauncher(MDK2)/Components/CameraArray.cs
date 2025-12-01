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
            public double Time { get; private set; }
            private double _lastUpdateTime;
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
                _cameras = AllGridBlocks.Where(b => b is IMyCameraBlock && b.CustomName.Contains($"Camera Array {ID}")).Cast<IMyCameraBlock>().ToList();
                if (_cameras.Count == 0)
                {
                    DebugWrite($"Error: Camera Array {ID} on has no cameras!", true);
                    throw new Exception($"Camera Array {ID} on has no cameras!");
                }
            }

            private void Init()
            {
                foreach (var camera in _cameras)
                {
                    camera.EnableRaycast = true;
                }
            }

            public void Update(double time)
            {
                Time = time;
                if (_lastUpdateTime == 0)
                    _lastUpdateTime = time;

                _totalAvailableRaycastDistance += (float)(time - _lastUpdateTime) * 2000f * _cameras.Count;
                _lastUpdateTime = time;
            }

            public MyDetectedEntityInfo Raycast(Vector3 raycastTarget)
            {
                if (CanScan(raycastTarget))
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

            public MyDetectedEntityInfo Raycast(Vector3 raycastTarget, float overshoot)
            {
                Vector3 raycastOvershoot = (raycastTarget - _cameras[_cameraIndex].GetPosition()) * overshoot;
                raycastTarget += raycastOvershoot;

                return Raycast(raycastTarget);
            }

            public bool CanScan(Vector3 raycastTarget)
            {
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

            public bool CanScan(Vector3 raycastTarget, float overshoot)
            {
                Vector3 raycastOvershoot = (raycastTarget - _cameras[_cameraIndex].GetPosition()) * overshoot;
                raycastTarget += raycastOvershoot;

                return CanScan(raycastTarget);
            }

            public Vector3 GetCameraPosition() => _cameras[_cameraIndex].GetPosition();
        }
    }
}
