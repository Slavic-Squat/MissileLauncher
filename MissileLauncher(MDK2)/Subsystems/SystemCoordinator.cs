using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
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
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {
        public class SystemCoordinator
        {
            public static double SystemTime { get; private set; }
            public static double GlobalTime { get; private set; }
            public static IMyShipController ReferenceController { get; private set; }
            public static Matrix ReferenceWorldMatrix => ReferenceController.WorldMatrix;
            public static Vector3 ReferencePosition => ReferenceController.GetPosition();
            public static Vector3 ReferenceVelocity => ReferenceController.GetShipVelocities().LinearVelocity;
            public static long SelfID => ReferenceController.CubeGrid.EntityId;
            public static EntityInfo SelfInfo { get; private set; }

            public bool IsMainClock { get; private set; } = true;
            public MyIni Config { get; private set; }
            public CommunicationHandler CommunicationHandler { get; private set; }
            public CommandHandler CommandHandler { get; private set; }
            public List<ControlStation> ControlStations { get; private set; }
            public List<TargetingLaser> TargetingLasers { get; private set; }
            public AWACS AWACS { get; private set; }
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public UICoordinator UICoordinator { get; private set; }

            private double _lastClockSync;

            public SystemCoordinator()
            {
                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                ReferenceController = AllGridBlocks.Find(b => b is IMyShipController && b.CustomName.Contains("Main Controller")) as IMyShipController;
                if (ReferenceController == null)
                {
                    DebugWrite($"Error: main controller not found!\n", true);
                    throw new Exception($"main controller not found!\n");
                }
            }

            private void Init()
            {
                Config = new MyIni();
                if (!Config.TryParse(MePb.CustomData))
                {
                    Config.Clear();
                }

                long secureBroadcastPIN = Config.Get("Config", "SecureBroadcastPIN").ToInt64(123456);
                Config.Set("Config", "SecureBroadcastPIN", secureBroadcastPIN);

                IsMainClock = Config.Get("Config", "IsMainClock").ToBoolean(true);
                Config.Set("Config", "IsMainClock", IsMainClock);

                CommandHandler = new CommandHandler();
                CommunicationHandler = new CommunicationHandler(0, secureBroadcastPIN);
                
                TargetingLasers = new List<TargetingLaser>();
                int numLasers = Config.Get("Targeting", "NumLasers").ToInt32(1);
                Config.Set("Targeting", "NumLasers", numLasers);
                for (int i = 0; i < numLasers; i++)
                {
                    float maxLaserDist = Config.Get("Targeting", $"Laser{i}MaxDistance").ToSingle(5000);
                    Config.Set("Targeting", $"Laser{i}MaxDistance", maxLaserDist);
                    float sensitivity = Config.Get("Targeting", $"Laser{i}Sensitivity").ToSingle(0.05f);
                    Config.Set("Targeting", $"Laser{i}Sensitivity", sensitivity);
                    TargetingLaser laser = new TargetingLaser(i, sensitivity, maxLaserDist);
                    laser.SyncRequested += SyncTarget;
                    TargetingLasers.Add(laser);
                }

                float maxAWACSDist = Config.Get("AWACS", "MaxDistance").ToSingle(5000);
                Config.Set("AWACS", "MaxDistance", maxAWACSDist);
                AWACS = new AWACS(0, maxAWACSDist);

                TargetCoordinator = new TargetCoordinator(0, CommunicationHandler);

                int numBays = Config.Get("Missiles", "NumBays").ToInt32(1);
                Config.Set("Missiles", "NumBays", numBays);
                MissileCoordinator = new MissileCoordinator(0, numBays, CommunicationHandler, TargetCoordinator.AllTargetsExt);

                UICoordinator = new UICoordinator(this);

                ControlStations = new List<ControlStation>();
                int numControlStations = Config.Get("Config", "NumControlStations").ToInt32(1);
                Config.Set("Config", "NumControlStations", numControlStations);
                for (int i = 0; i < numControlStations; i++)
                {
                    ControlStation controlStation = new ControlStation(i, UICoordinator);
                    ControlStations.Add(controlStation);
                }

                CommunicationHandler.RegisterBroadcastListener("FriendlyCommands", true);
                CommandHandler.RegisterCommand("SET_MAIN_CLOCK", (args) => SetMainClock(args[0]));
                CommandHandler.RegisterCommand("SYNC_CLOCK", (args) => SyncClock(args[0]));

                MePb.CustomData = Config.ToString();
            }

            public void Run()
            {
                SystemTime += RuntimeInfo.TimeSinceLastRun.TotalSeconds;
                GlobalTime += RuntimeInfo.TimeSinceLastRun.TotalSeconds;
                DebugEcho($"System Time: {SystemTime:F2}s\n");
                DebugWrite($"System Time: {SystemTime:F2}s\n", false);
                DebugEcho($"Last Run Time: {RuntimeInfo.LastRunTimeMs:F2}ms\n");
                DebugWrite($"Last Run Time: {RuntimeInfo.LastRunTimeMs:F2}ms\n", true);
                CommunicationHandler.Recieve();

                SelfInfo = new EntityInfo(SelfID, ReferencePosition, ReferenceVelocity, SystemTime);
                byte[] selfInfoData = SelfInfo.Serialize();

                CommunicationHandler.SendBroadcast(selfInfoData, "FriendlyInfo", true);

                foreach (var targetingLaser in TargetingLasers)
                {
                    targetingLaser.Run(SystemTime);
                }

                AWACS.Run(SystemTime);
                TargetCoordinator.Run(SystemTime);
                MissileCoordinator.Run(SystemTime);

                foreach (var target in AWACS.Targets.Values)
                {
                    TargetCoordinator.AddLocalTarget(target);
                }

                UICoordinator.Run();

                foreach (var controlStation in ControlStations)
                {
                    controlStation.Run(SystemTime);
                }

                if (IsMainClock && (SystemTime - _lastClockSync) > 10f)
                {
                    string command = $"SYNC_CLOCK {SystemTime}";
                    CommunicationHandler.SendBroadcast(command, "FriendlyCommands", true);
                    _lastClockSync = SystemTime;
                }

                while (CommunicationHandler.HasMessage("FriendlyCommands", true))
                {
                    MyIGCMessage msg;
                    if (CommunicationHandler.TryRetrieveMessage("FriendlyCommands", true, out msg))
                    {
                        string command = msg.Data as string;
                        Command(command);
                    }
                }
            }

            private void SyncTarget(TargetingLaser laser)
            {
                EntityInfoExt target = laser.Target;

                if (!target.IsValid) return;

                AWACS.AddTarget(target);
            }

            public void Command(string command)
            {
                CommandHandler.RunCommands(command);
            }

            private void SetMainClock(string boolString)
            {
                bool parsedBool = boolString.ToUpper() == "TRUE" || boolString == "1";
                IsMainClock = parsedBool;
            }

            private void SyncClock(string timeString)
            {
                if (IsMainClock) return;
                double time;
                if (double.TryParse(timeString, out time))
                {
                    GlobalTime = time;
                }
            }
        }
    }
}
