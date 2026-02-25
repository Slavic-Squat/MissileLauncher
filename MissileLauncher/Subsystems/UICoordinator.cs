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
            private Dictionary<long, EntityInfoExt> _allEntities = new Dictionary<long, EntityInfoExt>();

            public IReadOnlyDictionary<long, EntityInfoExt> AllTargets => _systemCoordinator.TargetCoordinator.AllTargets;
            public IReadOnlyDictionary<long, EntityInfoExt> AllMyMissiles => _systemCoordinator.MissileCoordinator.MyMissiles;
            public IReadOnlyDictionary<long, EntityInfoExt> AllEntities => _allEntities;

            public IReadOnlyDictionary<string, TargetingLaser> TargetingLasers => _systemCoordinator.TargetCoordinator.TargetingLasers;
            public MissileCoordinator MissileCoordinator => _systemCoordinator.MissileCoordinator;
            public TargetCoordinator TargetCoordinator => _systemCoordinator.TargetCoordinator;
            public IReadOnlyDictionary<string, MissileBay> MissileBays => _systemCoordinator.MissileCoordinator.MissileBays;
            public AWACS AWACS => _systemCoordinator.TargetCoordinator.AWACS;
            public TargetingDisplays TargetingDisplays { get; private set; }

            private int _runCounter = 0;

            public UICoordinator(SystemCoordinator systemCoordinator)
            {
                _systemCoordinator = systemCoordinator;
                TargetingDisplays = new TargetingDisplays(this);
            }

            public void Run()
            {
                if (_runCounter >= int.MaxValue) _runCounter = 0;
                _runCounter++;

                _allEntities.Clear();
                foreach (var target in AllTargets)
                {
                    _allEntities[target.Key] = target.Value;
                }
                foreach (var missile in AllMyMissiles)
                {
                    _allEntities[missile.Key] = missile.Value;
                }

                if (_runCounter % 10 == 0)
                {
                    TargetingDisplays.Draw();
                }
            }

            public void SelectBay(MissileBay bay, object caller) => MissileCoordinator.SelectBay(bay, caller);
            public void DeselectBay(MissileBay bay, object caller) => MissileCoordinator.DeselectBay(bay, caller);
            public void DeselectAll(object caller) => MissileCoordinator.DeselectAll(caller);
            public void SelectAll(object caller) => MissileCoordinator.SelectAll(caller);
            public void LaunchMissiles(long targetID, object caller) => MissileCoordinator.LaunchMissiles(targetID, caller);
            public void LaunchMissile(long targetID, object caller) => MissileCoordinator.LaunchMissile(targetID, caller);
            public void ForgetTarget(long targetID) => AWACS.RemoveTarget(targetID);
            public void AddTarget(long targetID) => AWACS.AddTarget(targetID);
            public void AbortMissile(long address, object caller) => MissileCoordinator.AbortMissile(address, caller);
            public void SetRelation(long entityID, EntityRelation relation) => TargetCoordinator.SetTargetRelation(entityID, relation);
        }
    }
}
