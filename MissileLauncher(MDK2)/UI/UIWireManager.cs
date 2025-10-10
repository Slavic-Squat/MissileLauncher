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
            public Dictionary<long, EntityInfoExt> AllMyMissilesExt => _systemCoordinator.MissileCoordinator.ActiveMissilesExt;

            public List<TargetingLaser> TargetingLasers => _systemCoordinator.TargetingLasers;
            public List<ControlStation> ControlStations => _systemCoordinator.ControlStations;
            public List<MissileBay> MissileBays => _systemCoordinator.MissileCoordinator.MissileBays;
            public HashSet<int> SelectedBays => _systemCoordinator.MissileCoordinator.SelectedBays;

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

            public Dictionary<long, EntityInfoExt> GetAllEntities()
            {
                var allEntities = new Dictionary<long, EntityInfoExt>(AllTargetsExt);
                foreach (var missile in AllMyMissilesExt)
                {
                    allEntities[missile.Key] = missile.Value;
                }

                return allEntities;
            }

            public void SelectBay(int bayID) => _systemCoordinator.MissileCoordinator.SelectBay(bayID);
            public void DeselectBay(int bayID) => _systemCoordinator.MissileCoordinator.DeselectBay(bayID);
            public void ClearSelectedBays() => _systemCoordinator.MissileCoordinator.ClearSelectedBays();
            public void LaunchMissiles(long targetID) => _systemCoordinator.MissileCoordinator.LaunchMissiles(targetID);
            public void ForgetTarget(long targetID) => _systemCoordinator.AWACS.RemoveTarget(targetID);
        }
    }
}
