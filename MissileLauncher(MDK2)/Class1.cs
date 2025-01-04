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
        public class TargetCoordinator
        {
            private Program program;
            private int ID;
            private string broadcastTag;
            private TargetingLaser targetingLaser;
            private AWACS awacs;
            private Dictionary<long, MyTuple<Vector3, Vector3, DateTime>> targets = new Dictionary<long, MyTuple<Vector3, Vector3, DateTime>>();

            public TargetCoordinator()
            {

            }

            public void Run(DateTime time)
            {
                program.IGC.SendBroadcastMessage
            }

            public void AddTarget(long targetID, Vector3 position, Vector3 velocity, DateTime time)
            {
                if (targets.ContainsKey(targetID))
                {
                    if (targets[targetID].Item3 < time)
                    {
                        targets[targetID] = new MyTuple<Vector3, Vector3, DateTime>(position, velocity, time);
                    }
                }
                else
                {
                    targets[targetID] = new MyTuple<Vector3, Vector3, DateTime>(position, velocity, time);
                }
            }

            public void AddTargets(Dictionary<long, MyTuple<Vector3, Vector3, DateTime>> targets)
            {
                foreach (var target in targets)
                {
                    AddTarget(target.Key, target.Value.Item1, target.Value.Item2, target.Value.Item3);
                }
            }

            public void RemoveTarget(long targetID)
            {
                targets.Remove(targetID);
            }

            public void RemoveTargets(List<long> targetIDs)
            {
                foreach (var targetID in targetIDs)
                {
                    targets.Remove(targetID);
                }
            }
        }
    }
}
