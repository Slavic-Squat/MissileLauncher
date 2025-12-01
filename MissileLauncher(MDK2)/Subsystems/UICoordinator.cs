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
        public class UICoordinator
        {
            private SystemCoordinator _systemCoordinator;

            public HashSet<long> NeutralIDs => _systemCoordinator.TargetCoordinator.NeutralIDs;
            public HashSet<long> FriendlyIDs => _systemCoordinator.TargetCoordinator.FriendlyIDs;
            public HashSet<long> HostileIDs => _systemCoordinator.TargetCoordinator.HostileIDs;
            public Dictionary<long, EntityInfoExt> AllTargets => _systemCoordinator.TargetCoordinator.AllTargetsExt;
            public Dictionary<long, EntityInfoExt> AllMyMissiles => _systemCoordinator.MissileCoordinator.MyMissilesExt;
            public Dictionary<long, EntityInfoExt> AllEntities { get; private set; } = new Dictionary<long, EntityInfoExt>();

            public List<TargetingLaser> TargetingLasers => _systemCoordinator.TargetingLasers;
            public List<ControlStation> ControlStations => _systemCoordinator.ControlStations;
            public MissileCoordinator MissileCoordinator => _systemCoordinator.MissileCoordinator;
            public TargetCoordinator TargetCoordinator => _systemCoordinator.TargetCoordinator;
            public List<MissileBay> MissileBays => _systemCoordinator.MissileCoordinator.MissileBays;
            public AWACS AWACS => _systemCoordinator.AWACS;
            public TargetingDisplays TargetingDisplays { get; private set; }

            public UICoordinator(SystemCoordinator systemCoordinator)
            {
                _systemCoordinator = systemCoordinator;
                TargetingDisplays = new TargetingDisplays(2, AllEntities);
            }

            public void Run()
            {
                AllEntities.Clear();
                foreach (var target in AllTargets)
                {
                    AllEntities[target.Key] = target.Value;
                }
                foreach (var missile in AllMyMissiles)
                {
                    AllEntities[missile.Key] = missile.Value;
                }

                TargetingDisplays.Run();
            }

            public void SelectBay(MissileBay bay, object caller) => _systemCoordinator.MissileCoordinator.SelectBay(bay, caller);
            public void DeselectBay(MissileBay bay, object caller) => _systemCoordinator.MissileCoordinator.DeselectBay(bay, caller);
            public void ClearSelectedBays(object caller) => _systemCoordinator.MissileCoordinator.ClearSelectedBays(caller);
            public void SelectAllBays(object caller) => _systemCoordinator.MissileCoordinator.SelectAllBays(caller);
            public void LaunchMissiles(long targetID, object caller) => _systemCoordinator.MissileCoordinator.LaunchMissiles(targetID, caller);
            public void LaunchMissile(long targetID, object caller) => _systemCoordinator.MissileCoordinator.LaunchMissile(targetID, caller);
            public void ForgetTarget(long targetID) => _systemCoordinator.AWACS.RemoveTarget(targetID);
            public void AbortMissile(long missileID, object caller) => _systemCoordinator.MissileCoordinator.AbortMissile(missileID, caller);
            public void SetRelation(long entityID, EntityRelation relation) => _systemCoordinator.TargetCoordinator.SetTargetRelation(entityID, relation);
        }
    }
}
