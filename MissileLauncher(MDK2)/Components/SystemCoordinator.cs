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
            public Program Program { get; private set; }
            public List<ControlStation> ControlStations { get; private set; }
            public List<TargetingLaser> TargetingLasers { get; private set; }
            public AWACS AWACS { get; private set; }
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileLauncher MissileLauncher { get; private set; }

            public TargetingSpriteBuilder TargetingSpriteBuilder { get; private set; }

            public IMyCubeGrid ReferenceGrid { get; private set; }

            private List<IEnumerator<bool>> _coroutines = new List<IEnumerator<bool>>(); 

            public SystemCoordinator(Program program, int numOfControlStations, int numOfTargetingLasers)
            {
                Program = program;

                for (int i = 0; i < numOfControlStations; i++)
                {
                    ControlStations.Add(new ControlStation(program, i, this));
                }

                for (int i = 0; i < numOfTargetingLasers;  i++)
                {
                    TargetingLasers.Add(new TargetingLaser(program, i));
                }

                AWACS = new AWACS(program, 0);
                TargetCoordinator = new TargetCoordinator(ReferenceGrid, "Coordinator0");
                MissileLauncher = new MissileLauncher(program, 0, ReferenceGrid.Name, "Launcher0", 1);
                TargetingSpriteBuilder = new TargetingSpriteBuilder(TargetCoordinator, ReferenceGrid, 30, 1, 100, 100000);
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

                }

                foreach (var targetingLaser in TargetingLasers)
                {
                    targetingLaser.Run(time);
                }

                AWACS.Run(time);
                TargetCoordinator.Run(time);
                TargetingSpriteBuilder.Run();
            }

            public IEnumerator<bool> ControlLaser(int laserID, ControlStation station)
            {
                TargetingLaser laser = TargetingLasers[laserID];
                UserInput input = station.UserInput;

                while (input.CHeld != true)
                {
                    laser.UserMoveLaser(input.MouseInput.X, input.MouseInput.Y);

                    if (input.SpacePress == true)
                    {
                        laser.UserFireLaser();
                    }
                    yield return true;
                }
                yield return false;
            }


        }
    }
}
