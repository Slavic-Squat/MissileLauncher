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

            public List<TargetingLaser> TargetingLasers => _systemCoordinator.TargetingLasers;
            public List<ControlStation> ControlStations => _systemCoordinator.ControlStations;

            public IMyCubeBlock ReferenceBlock => _systemCoordinator.ReferenceBlock;
            public long SelfID => _systemCoordinator.SelfID;

            public DateTime SystemTime => _systemCoordinator.Time;

            public UIWireManager(SystemCoordinator systemCoordinator)
            {
                _systemCoordinator = systemCoordinator;
            }

            public bool TakeControlOfLaser(int controlStationID, int targetingLaserID)
            {
                ControlStation controlStation = ControlStations[controlStationID];
                TargetingLaser targetingLaser = TargetingLasers[targetingLaserID];

                controlStation.TakeControl(targetingLaser);

                return true;
            }
            public Dictionary<long, EntityInfoExt> GetAllTargets() => _systemCoordinator.TargetCoordinator.GetAllTargets();
            public Dictionary<long, EntityInfoExt> GetAllMyMissiles() => _systemCoordinator.MissileCoordinator.GetAllMyMissiles();

            public Dictionary<long, EntityInfoExt> GetAllEntities()
            {
                var allEntities = new Dictionary<long, EntityInfoExt>(GetAllTargets());
                foreach (var missile in GetAllMyMissiles())
                {
                    allEntities[missile.Key] = missile.Value;
                }

                return allEntities;
            }
        }
    }
}
