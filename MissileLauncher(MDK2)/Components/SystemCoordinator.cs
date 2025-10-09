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
        public class SystemCoordinator
        {
            public List<ControlStation> ControlStations { get; private set; }
            public List<TargetingLaser> TargetingLasers { get; private set; }
            public AWACS AWACS { get; private set; }
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public CommunicationHandler CommunicationHandler { get; private set; }
            public IMyCubeBlock ReferenceBlock { get; private set; }
            public DateTime Time { get; private set; }
            public long SelfID => ReferenceBlock.CubeGrid.EntityId;
            public UIWireManager UIWireManager { get; private set; }

            private List<IEnumerator<bool>> _coroutines = new List<IEnumerator<bool>>(); 

            public SystemCoordinator(IMyCubeBlock referenceBlock, int numOfControlStations, int numOfTargetingLasers)
            {
                ReferenceBlock = referenceBlock;

                ControlStations = new List<ControlStation>();
                TargetingLasers = new List<TargetingLaser>();

                UIWireManager = new UIWireManager(this);

                for (int i = 0; i < numOfControlStations; i++)
                {
                    ControlStation controlStation = new ControlStation(i, UIWireManager);
                    ControlStations.Add(controlStation);
                }

                for (int i = 0; i < numOfTargetingLasers;  i++)
                {
                    TargetingLaser laser = new TargetingLaser(i);
                    laser.SyncRequested += SyncTarget;
                    TargetingLasers.Add(laser);
                }

                CommunicationHandler = new CommunicationHandler(0);
                AWACS = new AWACS(0);
                TargetCoordinator = new TargetCoordinator(0, SelfID, ReferenceBlock, CommunicationHandler);
                MissileCoordinator = new MissileCoordinator(0, 8, ReferenceBlock, SelfID, CommunicationHandler, TargetCoordinator.AllTargetsExt);
            }

            public void Run(DateTime time)
            {
                Time = time;
                CommunicationHandler.Recieve();

                for (int i = _coroutines.Count - 1; i >= 0; i--)
                {
                    var coroutine = _coroutines[i];
                    if (coroutine.MoveNext())
                    {
                        _coroutines.RemoveAt(i);
                    }
                }
                foreach (var controlStation in ControlStations)
                {
                    controlStation.Run(time);
                }

                foreach (var targetingLaser in TargetingLasers)
                {
                    targetingLaser.Run(time);
                }

                AWACS.Run(time);
                TargetCoordinator.Run(time);
                MissileCoordinator.Run(time);

                foreach (var target in AWACS.Targets.Values)
                {
                    TargetCoordinator.AddLocalTarget(target);
                }
            }

            public void SyncTarget(TargetingLaser laser)
            {
                EntityInfoExt target = laser.Target;
                DebugEcho("SYNCING");

                if (!target.IsEmpty)
                    AWACS.AddTarget(target);
            }
        }
    }
}
