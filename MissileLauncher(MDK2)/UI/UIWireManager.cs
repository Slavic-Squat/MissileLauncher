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
        public class UIWireManager
        {
            private SystemCoordinator _systemCoordinator;

            public HashSet<long> NeutralIDs => _systemCoordinator.TargetCoordinator.NeutralIDs;
            public HashSet<long> FriendlyIDs => _systemCoordinator.TargetCoordinator.FriendlyIDs;
            public HashSet<long> HostileIDs => _systemCoordinator.TargetCoordinator.HostileIDs;
            public Dictionary<long, EntityInfoExt> AllTargetsExt => _systemCoordinator.TargetCoordinator.AllTargetsExt;
            public Dictionary<long, EntityInfoExt> AllMyMissilesExt => _systemCoordinator.MissileCoordinator.MyMissilesExt;

            public List<TargetingLaser> TargetingLasers => _systemCoordinator.TargetingLasers;
            public List<ControlStation> ControlStations => _systemCoordinator.ControlStations;
            public MissileCoordinator MissileCoordinator => _systemCoordinator.MissileCoordinator;
            public TargetCoordinator TargetCoordinator => _systemCoordinator.TargetCoordinator;
            public List<MissileBay> MissileBays => _systemCoordinator.MissileCoordinator.MissileBays;

            public UIWireManager(SystemCoordinator systemCoordinator)
            {
                _systemCoordinator = systemCoordinator;
            }

            public Dictionary<long, EntityInfoExt> GetAllEntities()
            {
                var allEntities = new Dictionary<long, EntityInfoExt>(AllTargetsExt);
                foreach (var missile in AllMyMissilesExt)
                {
                    allEntities[missile.Key] = missile.Value;
                }

                return allEntities;
            }

            public bool SelectBay(MissileBay bay, object caller) => _systemCoordinator.MissileCoordinator.SelectBay(bay, caller);
            public bool DeselectBay(MissileBay bay, object caller) => _systemCoordinator.MissileCoordinator.DeselectBay(bay, caller);
            public bool ClearSelectedBays(object caller) => _systemCoordinator.MissileCoordinator.ClearSelectedBays(caller);
            public bool SelectAllBays(object caller) => _systemCoordinator.MissileCoordinator.SelectAllBays(caller);
            public bool LaunchMissiles(long targetID, object caller) => _systemCoordinator.MissileCoordinator.LaunchMissiles(targetID, caller);
            public bool LaunchMissile(long targetID, object caller) => _systemCoordinator.MissileCoordinator.LaunchMissile(targetID, caller);
            public bool ForgetTarget(long targetID) => _systemCoordinator.AWACS.RemoveTarget(targetID);
            public bool AbortMissile(long missileID, object caller) => _systemCoordinator.MissileCoordinator.AbortMissile(missileID, caller);
            public bool SetRelation(long entityID, EntityRelation relation) => _systemCoordinator.TargetCoordinator.SetTargetRelation(entityID, relation);
        }
    }
}
