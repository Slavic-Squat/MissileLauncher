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
    partial class Program : MyGridProgram
    {
        MissileLauncher missileLauncher;
        Dictionary<string, Action<int>> commands = new Dictionary<string, Action<int>>();
        MyCommandLine commandLine = new MyCommandLine();
        DateTime time;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            missileLauncher = new MissileLauncher(this, 0, 0);

            commands["QuickLaunch"] = _ => missileLauncher.LaunchNextAvailableMissile();
            commands["SyncTarget"] = _ => missileLauncher.SyncTarget();
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            time += Runtime.TimeSinceLastRun;
            Echo(time.ToString());
            missileLauncher.Run(time);

            if (commandLine.TryParse(argument))
            {
                string commandName = commandLine.Argument(0);
                string commandArgument = commandLine.Argument(1);
                Action<int> command;

                if (commandName != null && commandArgument != null)
                {
                    if (commands.TryGetValue(commandName, out command))
                    {
                        command(int.Parse(commandArgument));
                    }
                }
            }
        }
    }
}
