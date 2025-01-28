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
        private CommandHandler commandHandler;
        private Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();
        #endregion

        #region Broadcast Info
        private IMyBroadcastListener broadcastListener;
        private string channelTag;
        #endregion

        #region State Info
        private DateTime time;
        private bool mainClock = true;
        private bool listeningForClock = false;
        #endregion

        MissileLauncher missileLauncher;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            missileLauncher = new MissileLauncher(this, 0, "JombieLauncher", "Jombie268", 1);
            commands["SelectNextTarget"] = (args) => missileLauncher.SelectNextTarget();
            commands["SelectPrevTarget"] = (args) => missileLauncher.SelectPreviousTarget();
            commands["QuickInit"] = (args) => missileLauncher.InitNextAvailableMissile();
            commands["QuickLaunch"] = (args) => missileLauncher.LaunchNextAvailableMissile();
            commands["SyncTarget"] = (args) => missileLauncher.SyncTarget();
            commands["SyncClock"] = (args) => SyncClock(args[0]);
            commands["RecieveClock"] = (args) => RecieveClock(args[0]);
            commands["BroadcastClock"] = (args) => BroadcastClock(args[0]);

            commandHandler = new CommandHandler(Me, commands);
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            time += Runtime.TimeSinceLastRun;

            if (argument != null)
            {
                commandHandler.TryRunCommands(argument);
            }
            commandHandler.RunCustomDataCommands();

            if (!mainClock && listeningForClock && broadcastListener != null)
            {
                while (broadcastListener.HasPendingMessage)
                {
                    var message = broadcastListener.AcceptMessage();
                    if (message.Data is long)
                    {
                        time = new DateTime(message.As<long>());
                        listeningForClock = false;
                    }
                }
            }
            Echo(time.ToString());
            missileLauncher.Run(time);
        }

        public void SyncClock(string ticksString)
        {
            long ticks;
            long.TryParse(ticksString, out ticks);
            time = new DateTime(ticks);
        }

        public void BroadcastClock(string channelTag)
        {
            mainClock = true;
            this.channelTag = channelTag;
            IGC.SendBroadcastMessage(channelTag, time.Ticks);
        }

        public void RecieveClock(string channelTag)
        {
            mainClock = false;
            listeningForClock = true;
            this.channelTag = channelTag;
            broadcastListener = IGC.RegisterBroadcastListener(channelTag);
        }
    }
}
