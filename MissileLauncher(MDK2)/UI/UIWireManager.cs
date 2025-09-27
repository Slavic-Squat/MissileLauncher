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
            public UIWireManager(SystemCoordinator systemCoordinator)
            {
                _systemCoordinator = systemCoordinator;
            }

            public bool TakeControlOfLaser(int controlStationID, int targetingLaserID)
            {
                ControlStation controlStation = _systemCoordinator.ControlStations[controlStationID];
                TargetingLaser targetingLaser = _systemCoordinator.TargetingLasers[targetingLaserID];

                controlStation.TakeControl(targetingLaser);

                return true;
            }

            public IMyCubeBlock GetReferenceBlock() => _systemCoordinator.ReferenceBlock;
            public Dictionary<long, EntityInfo> GetAllEntities() => _systemCoordinator.TargetCoordinator.GetAllEntities();
            public Dictionary<long, EntityInfo> GetLocalEntities() => _systemCoordinator.TargetCoordinator.EntitiesLocal;
            public HashSet<long> GetLocalIDs() => _systemCoordinator.TargetCoordinator.EntitiesLocal.Keys.ToHashSet();
            public Dictionary<long, EntityInfo> GetRemoteEntities() => _systemCoordinator.TargetCoordinator.EntitiesRemote;
            public HashSet<long> GetRemoteIDs() => _systemCoordinator.TargetCoordinator.EntitiesRemote.Keys.ToHashSet();
            public HashSet<long> GetNeutralIDs() => _systemCoordinator.TargetCoordinator.NeutralIDs;
            public HashSet<long> GetHostileIDs() => _systemCoordinator.TargetCoordinator.HostileIDs;
            public HashSet<long> GetFriendlyIDs() => _systemCoordinator.TargetCoordinator.FriendlyIDs;
        }
    }
}
