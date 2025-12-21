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
            private PriorityQueue<IMyCameraBlock, double> _cameraQueue;
            public double Time { get; private set; }
            public string Name { get; private set; }
            public float MaxRaycastDistance { get; set; }
            public bool Recharging => _cameraQueue.Peek().AvailableScanRange < MaxRaycastDistance;
            public CameraArray(string name, float maxRaycastDistance)
            {
                Name = name.ToUpper();
                MaxRaycastDistance = maxRaycastDistance;

                GetBLocks();
                Init();
            }

            private void GetBLocks()
            {
                _cameras = AllGridBlocks.Where(b => b is IMyCameraBlock && b.CustomName.ToUpper().Contains(Name)).Cast<IMyCameraBlock>().ToList();
                if (_cameras.Count == 0)
                {
                    DebugWrite($"Error: {Name} Camera Array on has no cameras!\n", true);
                    throw new Exception($"{Name} Camera Array on has no cameras!\n");
                }
            }

            private void Init()
            {
                foreach (var camera in _cameras)
                {
                    camera.EnableRaycast = true;
                }

                Func<IMyCameraBlock, double> prioritySelector = c => -c.AvailableScanRange;
                _cameraQueue = new PriorityQueue<IMyCameraBlock, double>(prioritySelector, _cameras);
            }

            public MyDetectedEntityInfo Raycast(Vector3 raycastTarget)
            {
                if (CanScan(raycastTarget))
                {
                    IMyCameraBlock nextCamera = _cameraQueue.Dequeue();
                    var result = nextCamera.Raycast(raycastTarget);
                    float distanceUsed = Vector3.Distance(raycastTarget, nextCamera.GetPosition());
                    _cameraQueue.Enqueue(nextCamera);

                    return result;
                }
                else
                {
                    return default(MyDetectedEntityInfo);
                }
            }

            public MyDetectedEntityInfo Raycast(Vector3 raycastTarget, float overshoot)
            {
                Vector3 raycastOvershoot = (raycastTarget - GetCameraPosition()) * overshoot;
                raycastTarget += raycastOvershoot;

                return Raycast(raycastTarget);
            }

            public bool CanScan(Vector3 raycastTarget)
            {
                IMyCameraBlock nextCamera = _cameraQueue.Peek();
                float raycastDistance = Vector3.Distance(raycastTarget, nextCamera.GetPosition());

                if (nextCamera.CanScan(raycastTarget) && raycastDistance < MaxRaycastDistance)
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
                Vector3 raycastOvershoot = (raycastTarget - GetCameraPosition()) * overshoot;
                raycastTarget += raycastOvershoot;

                return CanScan(raycastTarget);
            }

            public Vector3 GetCameraPosition() => _cameraQueue.Peek().GetPosition();
        }
    }
}
