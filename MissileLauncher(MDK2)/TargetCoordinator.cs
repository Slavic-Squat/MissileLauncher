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
            #region General Info
            private Program program;
            private int ID;
            private string name;
            #endregion

            #region Parts
            private IMyShipController launcher;
            #endregion

            #region Broadcast Info
            private string launcherTag;
            private IMyBroadcastListener missilesInfoListener;
            #endregion

            #region Output
            public MyTuple<string, Vector3, Vector3, long> launcherInfo;
            public Dictionary<long, MyTuple<Vector3, Vector3, long>> targetsInfo = new Dictionary<long, MyTuple<Vector3, Vector3, long>>();
            public List<long> targetIDs = new List<long>();
            public Dictionary<string, MyTuple<MyTuple<string, long, long>, MyTuple<Vector3, Vector3, Vector3>>> missilesInfo = new Dictionary<string, MyTuple<MyTuple<string, long, long>, MyTuple<Vector3, Vector3, Vector3>>>();
            public List<string> missileTags = new List<string>();
            #endregion

            public TargetCoordinator(Program program, int ID, IMyShipController launcher, string name, string launcherTag)
            {
                this.program = program;
                this.ID = ID;
                this.launcher = launcher;
                this.name = name;
                this.launcherTag = launcherTag;

                missilesInfoListener = program.IGC.RegisterBroadcastListener($"[{launcherTag}]_MissilesInfo");
            }

            public void Run(DateTime time)
            {
                while (missilesInfoListener.HasPendingMessage)
                {
                    var messageIn = missilesInfoListener.AcceptMessage();
                    if (messageIn.Data is MyTuple<MyTuple<string, string, long, long>, MyTuple<Vector3, Vector3, Vector3>>)
                    {
                        var missileInfo = messageIn.As<MyTuple<MyTuple<string, string, long, long>, MyTuple<Vector3, Vector3, Vector3>>>();
                        AddMissile(missileInfo.Item1.Item1, missileInfo.Item1.Item2, missileInfo.Item1.Item3, missileInfo.Item1.Item4, missileInfo.Item2.Item1, missileInfo.Item2.Item2, missileInfo.Item2.Item3);
                    }
                }

                for (int i = missileTags.Count - 1; i >= 0; i--)
                {
                    var missileTag = missileTags[i];
                    TimeSpan timeSinceLastUpdate = time - new DateTime(missilesInfo[missileTag].Item1.Item3);

                    if (timeSinceLastUpdate.TotalSeconds > 5)
                    {
                        RemoveMissile(missileTag);
                    }
                }
                for (int i = targetIDs.Count - 1; i >= 0; i--)
                {
                    var targetID = targetIDs[i];
                    TimeSpan timeSinceLastDetection = time - new DateTime(targetsInfo[targetID].Item3);

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveTarget(targetID);
                    }
                }
                launcherInfo = new MyTuple<string, Vector3, Vector3, long>(name, launcher.GetPosition(), launcher.GetShipVelocities().LinearVelocity, time.Ticks);
                var messageOut0 = targetsInfo.ToImmutableDictionary();
                var messageOut1 = launcherInfo;
                program.IGC.SendBroadcastMessage($"[{launcherTag}]_TargetsInfo", messageOut0);
                program.IGC.SendBroadcastMessage($"[{launcherTag}]_LauncherInfo", messageOut1);
            }

            public void AddTarget(long targetID, Vector3 position, Vector3 velocity, DateTime time)
            {
                if (targetIDs.Contains(targetID))
                {
                    if (targetsInfo[targetID].Item3 < time.Ticks)
                    {
                        targetsInfo[targetID] = new MyTuple<Vector3, Vector3, long>(position, velocity, time.Ticks);
                    }
                }
                else
                {
                    targetsInfo[targetID] = new MyTuple<Vector3, Vector3, long>(position, velocity, time.Ticks);
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

            public void AddMissile(string missileTag, string stage, long targetID, long timeTicks, Vector3 position, Vector3 velocity, Vector3 headingVector)
            {
                if (!missileTags.Contains(missileTag))
                {
                    missileTags.Add(missileTag);
                }
                missilesInfo[missileTag] = new MyTuple<MyTuple<string, long, long>, MyTuple<Vector3, Vector3, Vector3>>()
                {
                    Item1 = new MyTuple<string, long, long>(stage, targetID, timeTicks),
                    Item2 = new MyTuple<Vector3, Vector3, Vector3>(position, velocity, headingVector)
                };
            }

            public void RemoveMissile(string missileTag)
            {
                missilesInfo.Remove(missileTag);
                missileTags.Remove(missileTag);
            }
        }
    }
}
