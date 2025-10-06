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
        #region Command Control
        private CommandHandler _commandHandler;
        private Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();
        #endregion

        #region State Info
        private DateTime time = DateTime.Now;
        #endregion

        private SystemCoordinator _systemCoordinator;
        public static Action<string> debugEcho;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            List<IMyShipController> tempList = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType(tempList, ctrl => ctrl.IsMainCockpit);
            IMyCubeBlock referenceBlock = tempList.Count > 0 ? tempList[0] as IMyCubeBlock : null;

            _systemCoordinator = new SystemCoordinator(this, referenceBlock, 1, 1);

            _commandHandler = new CommandHandler(Me, commands);

            debugEcho = Echo;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            time += Runtime.TimeSinceLastRun;

            if (argument != null)
            {
                _commandHandler.TryRunCommands(argument);
            }
            _commandHandler.RunCustomDataCommands();
            _systemCoordinator.Run(time);
            Echo(time.ToString());
        }
    }
}
