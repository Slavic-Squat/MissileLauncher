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

            private UIWireManager _uiWireManager;
            private Program _program;
            private List<IEnumerator<bool>> _coroutines = new List<IEnumerator<bool>>(); 

            public SystemCoordinator(Program program, IMyCubeBlock referenceBlock, int numOfControlStations, int numOfTargetingLasers)
            {
                _program = program;
                ReferenceBlock = referenceBlock;

                ControlStations = new List<ControlStation>();
                TargetingLasers = new List<TargetingLaser>();

                _uiWireManager = new UIWireManager(this);

                for (int i = 0; i < numOfControlStations; i++)
                {
                    ControlStations.Add(new ControlStation(program, i, _uiWireManager));
                }

                for (int i = 0; i < numOfTargetingLasers;  i++)
                {
                    TargetingLasers.Add(new TargetingLaser(program, i));
                }

                CommunicationHandler = new CommunicationHandler(program, 0);
                AWACS = new AWACS(program, 0);
                TargetCoordinator = new TargetCoordinator(0, SelfID, ReferenceBlock, CommunicationHandler);
                MissileCoordinator = new MissileCoordinator(program, 0, 0, ReferenceBlock, SelfID, CommunicationHandler);
            }

            public void Run(DateTime time)
            {
                Time = time;
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

                SyncTarget(0);

                foreach (var target in AWACS.Targets.Values)
                {
                    TargetCoordinator.AddLocalTarget(target);
                }
            }

            public void SyncTarget(int laserID)
            {
                EntityInfoExt target = TargetingLasers[laserID].Target;

                if (!target.IsEmpty)
                    AWACS.AddTarget(target);
            }
        }
    }
}
