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
            private MovingAverage _avgRaycastDistance = new MovingAverage(10);
            private double _timeLastRaycast;
            public double Time { get; private set; }
            public string ID { get; private set; }
            public float MaxRaycastDistance { get; set; }
            public bool Recharging => Time - _timeLastRaycast < TimeBetweenRaycasts;
            public int CameraCount => _cameras.Count;
            public double TimeBetweenRaycasts => _avgRaycastDistance.Average / (_cameras[0].RaycastTimeMultiplier * 1000);
            public double Frequency => 1 / TimeBetweenRaycasts;
            public CameraArray(string id, float maxRaycastDistance)
            {
                ID = id.ToUpper();
                MaxRaycastDistance = maxRaycastDistance;

                GetBLocks();
                Init();
            }

            private void GetBLocks()
            {
                _cameras = AllGridBlocks.Where(b => b is IMyCameraBlock && b.CustomName.ToUpper().Contains(ID)).Cast<IMyCameraBlock>().ToList();
                if (_cameras.Count == 0)
                {
                    DebugWrite($"Error: {ID} Camera Array on has no cameras!\n", true);
                    throw new Exception($"{ID} Camera Array on has no cameras!\n");
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

            public void Update(double time)
            {
                Time = time;
            }

            public MyDetectedEntityInfo Raycast(Vector3 raycastTarget)
            {
                if (CanScan(raycastTarget))
                {
                    IMyCameraBlock nextCamera = _cameraQueue.Dequeue();
                    var result = nextCamera.Raycast(raycastTarget);
                    float raycastDistance = Vector3.Distance(raycastTarget, nextCamera.GetPosition());
                    _avgRaycastDistance.Add(raycastDistance);
                    _cameraQueue.Enqueue(nextCamera);
                    _timeLastRaycast = Time;
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

                if (nextCamera.CanScan(raycastTarget) && raycastDistance < MaxRaycastDistance && !Recharging)
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
