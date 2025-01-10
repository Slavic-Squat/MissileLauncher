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
        private CommandHandler commandHandler;
        private Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();

        private DateTime time;
        private IMyBroadcastListener broadcastListener;
        private string broadcastTag;
        private bool mainClock = true;
        private bool listeningForClock = false;

        MissileLauncher missileLauncher;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            missileLauncher = new MissileLauncher(this, 0, "JombieLauncher", "Jombie268", 0);

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
            if (argument != null)
            {
                commandHandler.TryRunCommand(argument);
            }
            commandHandler.Run();

            time += Runtime.TimeSinceLastRun;
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

        public void BroadcastClock(string channel)
        {
            mainClock = true;
            broadcastTag = channel;
            IGC.SendBroadcastMessage(channel, time.Ticks);
        }

        public void RecieveClock(string channel)
        {
            mainClock = false;
            listeningForClock = true;
            broadcastTag = channel;
            broadcastListener = IGC.RegisterBroadcastListener(channel);
        }
    }
}
