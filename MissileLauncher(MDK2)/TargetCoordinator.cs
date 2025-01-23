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
            #region Parts
            private IMyShipController _launcherController;
            private IMyBroadcastListener _missilesInfoListener;
            #endregion

            #region Fields
            private Dictionary<long, MyTuple<string, long, Vector3, Vector3, long>> _targetsIGC = new Dictionary<long, MyTuple<string, long, Vector3, Vector3, long>>();
            #endregion

            #region Properties
            public Program Program { get; private set; }
            public int ID { get; private set; }
            public string Name { get; private set; }
            public string LauncherTag { get; private set; }
            public LauncherInfo Launcher {  get; private set; }
            public Dictionary<long, TargetInfo> Targets {  get; private set; }
            public List<long> TargetIDs { get; private set; }
            public Dictionary<long, MissileInfo> Missiles { get; private set; }
            public List<long> MissileIDs { get; private set; }
            #endregion

            public TargetCoordinator(Program program, int id, IMyShipController launcherController, string name, string coordinatorTag)
            {
                Program = program;
                ID = id;
                _launcherController = launcherController;
                Name = name;
                LauncherTag = coordinatorTag;

                Targets = new Dictionary<long, TargetInfo>();
                TargetIDs = new List<long>();
                Missiles = new Dictionary<long, MissileInfo>();
                MissileIDs = new List<long>();

                _missilesInfoListener = Program.IGC.RegisterBroadcastListener($"[{LauncherTag}]_MissilesInfo");
            }

            public void Run(DateTime time)
            {
                while (_missilesInfoListener.HasPendingMessage)
                {
                    var messageIn = _missilesInfoListener.AcceptMessage();
                    if (messageIn.Data is MyTuple<MyTuple<long, long, Vector3, Vector3, long>, MyTuple<string, string>>)
                    {
                        var missileInfo = messageIn.As<MyTuple<MyTuple<long, long, Vector3, Vector3, long>, MyTuple<string, string>>>();
                        AddMissile(MissileInfo.FromIGC(missileInfo));
                    }
                }

                for (int i = MissileIDs.Count - 1; i >= 0; i--)
                {
                    var missileID = MissileIDs[i];
                    TimeSpan timeSinceLastUpdate = time - Missiles[missileID].TimeRecorded;

                    if (timeSinceLastUpdate.TotalSeconds > 5)
                    {
                        RemoveMissile(missileID);
                    }
                }
                for (int i = TargetIDs.Count - 1; i >= 0; i--)
                {
                    var targetID = TargetIDs[i];
                    TimeSpan timeSinceLastDetection = time - Targets[targetID].TimeRecorded;

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveTarget(targetID);
                    }
                }
                Launcher = new LauncherInfo(Name, _launcherController.CubeGrid.EntityId, _launcherController.GetPosition(), _launcherController.GetShipVelocities().LinearVelocity, time);
                _targetsIGC.Clear();
                foreach (var target in Targets)
                {
                    _targetsIGC.Add(target.Key, TargetInfo.ToIGC(target.Value));
                }
                var messageOut0 = _targetsIGC.ToImmutableDictionary();
                var messageOut1 = LauncherInfo.ToIGC(Launcher);
                Program.IGC.SendBroadcastMessage($"[{LauncherTag}]_TargetsInfo", messageOut0);
                Program.IGC.SendBroadcastMessage($"[{LauncherTag}]_LauncherInfo", messageOut1);
            }

            public void AddTarget(TargetInfo target)
            {
                if (TargetIDs.Contains(target.EntityID))
                {
                    if (Targets[target.EntityID].TimeRecorded < target.TimeRecorded)
                    {
                        Targets[target.EntityID] = target;
                    }
                }
                else
                {
                    Targets[target.EntityID] = target;
                    TargetIDs.Add(target.EntityID);
                }
            }

            public void AddTargets(Dictionary<long, TargetInfo> targets)
            {
                foreach (var target in targets.Values)
                {
                    AddTarget(target);
                }
            }

            public void RemoveTarget(long targetID)
            {
                Targets.Remove(targetID);
                TargetIDs.Remove(targetID);
            }

            public void RemoveTargets(List<long> TargetIDs)
            {
                foreach (var targetID in TargetIDs)
                {
                    RemoveTarget(targetID);
                }
            }

            public void AddMissile(MissileInfo missile)
            {
                if (!MissileIDs.Contains(missile.EntityID))
                {
                    MissileIDs.Add(missile.EntityID);
                }
                Missiles[missile.EntityID] = missile;
            }

            public void RemoveMissile(long missileID)
            {
                Missiles.Remove(missileID);
                MissileIDs.Remove(missileID);
            }
        }
    }
}
