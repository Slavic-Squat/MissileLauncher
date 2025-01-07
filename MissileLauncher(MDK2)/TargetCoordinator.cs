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
            public Dictionary<long, MyTuple<Vector3, Vector3, long>> targetsInfo = new Dictionary<long, MyTuple<Vector3, Vector3, long>>();
            public List<long> targetIDs = new List<long>();

            public TargetCoordinator(Program program, int ID, string broadcastTag)
            {
                this.program = program;
                this.ID = ID;
                this.broadcastTag = broadcastTag;
            }

            public void Run(DateTime time)
            {
                for (int i = targetIDs.Count - 1; i >= 0; i--)
                {
                    var targetID = targetIDs[i];
                    TimeSpan timeSinceLastDetection = time - new DateTime(targetsInfo[targetID].Item3);

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveTarget(targetID);
                    }
                }
                var message = targetsInfo.ToImmutableDictionary();
                program.IGC.SendBroadcastMessage($"{broadcastTag}_TargetInfo", message);
            }

            public void AddTarget(long targetID, Vector3 position, Vector3 velocity, DateTime time)
            {
                if (targetsInfo.ContainsKey(targetID))
                {
                    if (targetsInfo[targetID].Item3 < time.Ticks)
                    {
                        targetsInfo[targetID] = new MyTuple<Vector3, Vector3, long>(position, velocity, time.Ticks);
                    }
                }
                else
                {
                    targetsInfo[targetID] = new MyTuple<Vector3, Vector3, long>(position, velocity, time.Ticks);
                }

                if (!targetIDs.Contains(targetID))
                {
                    targetIDs.Add(targetID);
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
                targetsInfo.Remove(targetID);
                targetIDs.Remove(targetID);
            }

            public void RemoveTargets(List<long> targetIDs)
            {
                foreach (var targetID in targetIDs)
                {
                    RemoveTarget(targetID);
                }
            }
        }
    }
}
