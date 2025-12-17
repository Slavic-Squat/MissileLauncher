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
            public static double GlobalTime { get; private set; }
            public static IMyShipController ReferenceController { get; private set; }
            public static Matrix ReferenceWorldMatrix => ReferenceController.WorldMatrix;
            public static Vector3 ReferencePosition => ReferenceController.GetPosition();
            public static Vector3 ReferenceVelocity => ReferenceController.GetShipVelocities().LinearVelocity;
            public static long SelfID => ReferenceController.CubeGrid.EntityId;
            public static EntityInfo SelfInfo { get; private set; }

            public bool IsMainClock { get; private set; } = true;
            public List<ControlStation> ControlStations { get; private set; }
            public List<TargetingLaser> TargetingLasers { get; private set; }
            public AWACS AWACS { get; private set; }
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public UICoordinator UICoordinator { get; private set; }
            public double Time { get; private set; }

            private double _lastClockSync;
            private double _globalTimeOffset;

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
                IsMainClock = Config.Get("Config", "IsMainClock").ToBoolean(true);
                Config.Set("Config", "IsMainClock", IsMainClock);

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

                TargetCoordinator = new TargetCoordinator();

                int numBays = Config.Get("Missiles", "NumBays").ToInt32(1);
                Config.Set("Missiles", "NumBays", numBays);
                MissileCoordinator = new MissileCoordinator(numBays, TargetCoordinator.AllTargetsExt);

                UICoordinator = new UICoordinator(this);

                ControlStations = new List<ControlStation>();
                int numControlStations = Config.Get("Config", "NumControlStations").ToInt32(1);
                Config.Set("Config", "NumControlStations", numControlStations);
                for (int i = 0; i < numControlStations; i++)
                {
                    ControlStation controlStation = new ControlStation(i, UICoordinator);
                    ControlStations.Add(controlStation);
                }

                CommunicationHandler0.RegisterBroadcastListener("FriendlyCommands", true);
                CommandHandler0.RegisterCommand("SET_MAIN_CLOCK", (args) => SetMainClock(args[0]));
                CommandHandler0.RegisterCommand("SYNC_CLOCK", (args) => SyncClock(args[0]));
                CommandHandler0.RegisterCommand("PAUSE_CONTROL_STATION", (args) => PauseControlStation(args[0]));
                CommandHandler0.RegisterCommand("RESUME_CONTROL_STATION", (args) => ResumeControlStation(args[0]));
            }

            public void Run(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }

                GlobalTime = time + _globalTimeOffset;
                DebugEcho($"Global Time: {GlobalTime:F2}s");

                SelfInfo = new EntityInfo(SelfID, ReferencePosition, ReferenceVelocity, GlobalTime);
                byte[] selfInfoBytes = SelfInfo.Serialize();

                CommunicationHandler0.SendBroadcast(selfInfoBytes, "FriendlyInfo", true);

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

                UICoordinator.Run();

                foreach (var controlStation in ControlStations)
                {
                    controlStation.Run(time);
                }

                if (IsMainClock && (time - _lastClockSync) > 10f)
                {
                    string command = $"SYNC_CLOCK {time}";
                    CommunicationHandler0.SendBroadcast(command, "FriendlyCommands", true);
                    _lastClockSync = time;
                }

                while (CommunicationHandler0.HasMessage("FriendlyCommands", true))
                {
                    MyIGCMessage msg;
                    if (CommunicationHandler0.TryRetrieveMessage("FriendlyCommands", true, out msg))
                    {
                        string command = msg.Data as string;
                        CommandHandler0.RunCommands(command);
                    }
                }

                Time = time;
            }

            private void SyncTarget(TargetingLaser laser)
            {
                EntityInfoExt target = laser.Target;

                if (!target.IsValid) return;

                AWACS.AddTarget(target);
            }

            private void SetMainClock(string boolString)
            {
                bool isMain;
                if (!bool.TryParse(boolString, out isMain))
                {
                    return;
                }
                IsMainClock = isMain;
            }

            private void SyncClock(string timeString)
            {
                if (IsMainClock) return;
                double time;
                if (!double.TryParse(timeString, out time))
                {
                    return;
                }
                _globalTimeOffset = time - Time;
            }

            private void PauseControlStation(string idString)
            {
                int id;
                if (!int.TryParse(idString, out id))
                {
                    return;
                }
                if (id < 0 || id >= ControlStations.Count)
                {
                    return;
                }
                ControlStations[id].PauseControl();
            }

            private void ResumeControlStation(string idString)
            {
                int id;
                if (!int.TryParse(idString, out id))
                {
                    return;
                }
                if (id < 0 || id >= ControlStations.Count)
                {
                    return;
                }
                ControlStations[id].ResumeControl();
            }
        }
    }
}
