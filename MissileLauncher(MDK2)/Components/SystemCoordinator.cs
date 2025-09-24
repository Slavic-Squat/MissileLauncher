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
            public MissileLauncher MissileLauncher { get; private set; }
            public CommunicationHandler CommunicationHandler { get; private set; }
            public TargetingSpriteBuilder TargetingSpriteBuilder { get; private set; }

            private IMyCubeGrid _referenceGrid;
            private Program _program;
            private List<IEnumerator<bool>> _coroutines = new List<IEnumerator<bool>>(); 

            public SystemCoordinator(Program program, int numOfControlStations, int numOfTargetingLasers)
            {
                _program = program;
                _referenceGrid = program.Me.CubeGrid;

                ControlStations = new List<ControlStation>();
                TargetingLasers = new List<TargetingLaser>();

                for (int i = 0; i < numOfControlStations; i++)
                {
                    ControlStations.Add(new ControlStation(program, i));
                }

                for (int i = 0; i < numOfTargetingLasers;  i++)
                {
                    TargetingLasers.Add(new TargetingLaser(program, i));
                }

                CommunicationHandler = new CommunicationHandler(program, 0);
                AWACS = new AWACS(program, 0);
                TargetCoordinator = new TargetCoordinator(_referenceGrid, CommunicationHandler);
                //MissileLauncher = new MissileLauncher(program, 0, 1);
                //TargetingSpriteBuilder = new TargetingSpriteBuilder(_referenceGrid, TargetCoordinator.AllEntities, TargetCoordinator.FriendlyIDs, TargetCoordinator.HostileIDs, TargetCoordinator.NeutralIDs, _referenceGrid.EntityId);
            }

            public void Run(DateTime time)
            {
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
                //TargetingSpriteBuilder.Run();
            }

            public void SyncTarget(int laserID)
            {
                EntityInfo target = TargetingLasers[laserID].Target;

                if (target != null)
                    AWACS.AddTarget(target);
            }
        }
    }
}
