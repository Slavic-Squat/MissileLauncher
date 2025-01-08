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
        Dictionary<string, Action<string>> commands = new Dictionary<string, Action<string>>();
        MyCommandLine commandLine = new MyCommandLine();
        DateTime time;
        bool mainClock = true;
        bool listeningForClock = false;
        string broadcastTag;
        IMyBroadcastListener broadcastListener;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            missileLauncher = new MissileLauncher(this, 0, "JombieLauncher", "Jombie268", 0);

            commands["QuickLaunch"] = _ => missileLauncher.LaunchNextAvailableMissile();
            commands["SyncTarget"] = _ => missileLauncher.SyncTarget();
            commands["SyncClock"] = SyncClock;
            commands["RecieveClock"] = RecieveClock;
            commands["BroadcastClock"] = BroadcastClock;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
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

            if (commandLine.TryParse(argument))
            {
                string commandName = commandLine.Argument(0);
                string commandArgument = commandLine.Argument(1);
                Action<string> command;

                if (commands.TryGetValue(commandName, out command))
                {
                    try
                    {
                        command(commandArgument);
                    }
                    catch (Exception ex)
                    {
                        Echo("Command had incorrect parameters");
                    }
                }
            }
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
