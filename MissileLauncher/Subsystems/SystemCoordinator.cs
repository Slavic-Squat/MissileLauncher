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
            public static MatrixD ReferenceWorldMatrix => ReferenceController.WorldMatrix;
            public static Vector3D ReferencePosition => ReferenceController.GetPosition();
            public static Vector3D ReferenceVelocity => ReferenceController.GetShipVelocities().LinearVelocity;
            public static long SelfID => ReferenceController.CubeGrid.EntityId;
            public static EntityInfo SelfInfo { get; private set; }

            private Dictionary<string, ControlStation> _controlStations = new Dictionary<string, ControlStation>();
            private Dictionary<string, TargetingLaser> _targetingLasers = new Dictionary<string, TargetingLaser>();

            private bool _isMainClock = true;
            private double _lastClockSync;
            private double _globalTimeOffset;
            private double _time;

            public AWACS AWACS { get; private set; }
            public IReadOnlyDictionary<string, TargetingLaser> TargetingLasers => _targetingLasers;
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public UICoordinator UICoordinator { get; private set; }

            public SystemCoordinator()
            {
                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                ReferenceController = AllGridBlocks.Where(b => b is IMyShipController && b.CustomName.ToUpper().Contains("MAIN CONTROLLER")).FirstOrDefault() as IMyShipController;
                if (ReferenceController == null)
                {
                    DebugWrite($"Error: main controller not found!\n", true);
                    throw new Exception($"main controller not found!\n");
                }
            }

            private void Init()
            {
                _isMainClock = Config.Get("Config", "IsMainClock").ToBoolean(true);
                Config.Set("Config", "IsMainClock", _isMainClock);

                int numLasers = Config.Get("Targeting", "NumLasers").ToInt32(1);
                Config.Set("Targeting", "NumLasers", numLasers);
                for (int i = 0; i < numLasers; i++)
                {
                    string id = i.ToString().ToUpper();
                    float maxLaserDist = Config.Get("Targeting", $"Laser{id}MaxDistance").ToSingle(5000);
                    Config.Set("Targeting", $"Laser{id}MaxDistance", maxLaserDist);
                    float sensitivity = Config.Get("Targeting", $"Laser{id}Sensitivity").ToSingle(0.05f);
                    Config.Set("Targeting", $"Laser{id}Sensitivity", sensitivity);
                    TargetingLaser laser = new TargetingLaser(id, sensitivity, maxLaserDist);
                    laser.SyncRequested += SyncTarget;
                    _targetingLasers[id] = laser;
                }

                bool hasAWACS = Config.Get("AWACS", "Enabled").ToBoolean(true);
                Config.Set("AWACS", "Enabled", hasAWACS);
                float maxAWACSDist = Config.Get("AWACS", "MaxDistance").ToSingle(5000);
                Config.Set("AWACS", "MaxDistance", maxAWACSDist);
                if (hasAWACS)
                {
                    AWACS = new AWACS(maxAWACSDist);
                }

                TargetCoordinator = new TargetCoordinator();

                int numBays = Config.Get("Missiles", "NumBays").ToInt32(1);
                Config.Set("Missiles", "NumBays", numBays);
                MissileCoordinator = new MissileCoordinator(numBays, TargetCoordinator.AllTargetsExt);

                UICoordinator = new UICoordinator(this);

                int numControlStations = Config.Get("Config", "NumControlStations").ToInt32(1);
                Config.Set("Config", "NumControlStations", numControlStations);
                for (int i = 0; i < numControlStations; i++)
                {
                    string id = i.ToString().ToUpper();
                    ControlStation controlStation = new ControlStation(id, UICoordinator);
                    _controlStations[id] = controlStation;
                }

                CommunicationHandler0.RegisterBroadcastListener("FRIENDLY_COMMANDS", true);
                CommandHandler0.RegisterCommand("SET_MAIN_CLOCK", (args) => { if (args.Length > 0) SetMainClock(args[0]); });
                CommandHandler0.RegisterCommand("SYNC_CLOCK", (args) => { if (args.Length > 0) SyncClock(args[0]); });
                CommandHandler0.RegisterCommand("PAUSE_CONTROL_STATION", (args) => { if (args.Length > 0) PauseControlStation(args[0]); });
                CommandHandler0.RegisterCommand("RESUME_CONTROL_STATION", (args) => { if (args.Length > 0) ResumeControlStation(args[0]); });
                CommandHandler0.RegisterCommand("QUICK_LAUNCH", (args) => { if (args.Length > 0) QuickLaunch(args[0]); });
            }

            public void Run(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                GlobalTime = time + _globalTimeOffset;
                DebugEcho($"Global Time: {GlobalTime:F2}s");

                SelfInfo = new EntityInfo(SelfID, ReferencePosition, ReferenceVelocity, GlobalTime);
                byte[] selfInfoBytes = SelfInfo.Serialize();

                CommunicationHandler0.SendBroadcast(selfInfoBytes, "FRIENDLY_INFO", true);

                foreach (var targetingLaser in _targetingLasers.Values)
                {
                    targetingLaser.Run(time);
                    TargetCoordinator.AddLocalTarget(targetingLaser.Target);
                }

                if (AWACS != null)
                {
                    AWACS.Run(time);
                    foreach (var target in AWACS.Targets.Values)
                    {
                        TargetCoordinator.AddLocalTarget(target);
                    }
                }

                TargetCoordinator.Run(time);
                MissileCoordinator.Run(time);
                UICoordinator.Run();

                foreach (var controlStation in _controlStations.Values)
                {
                    controlStation.Run(time);
                }

                if (_isMainClock && (time - _lastClockSync) > 10f)
                {
                    string command = $"SYNC_CLOCK {time}";
                    CommunicationHandler0.SendBroadcast(command, "FRIENDLY_COMMANDS", true);
                    _lastClockSync = time;
                }

                while (CommunicationHandler0.HasMessage("FRIENDLY_COMMANDS", true))
                {
                    MyIGCMessage msg;
                    if (CommunicationHandler0.TryRetrieveMessage("FRIENDLY_COMMANDS", true, out msg))
                    {
                        string command = msg.Data as string;
                        CommandHandler0.RunCommands(command);
                    }
                }

                _time = time;
            }

            private void SyncTarget(TargetingLaser laser)
            {
                EntityInfoExt target = laser.Target;

                if (!target.IsValid || AWACS == null) return;

                AWACS.AddTarget(target);
            }

            private void SetMainClock(string boolString)
            {
                bool isMain;
                if (!bool.TryParse(boolString, out isMain))
                {
                    return;
                }
                _isMainClock = isMain;
            }

            private void SyncClock(string timeString)
            {
                if (_isMainClock) return;
                double time;
                if (!double.TryParse(timeString, out time))
                {
                    return;
                }
                _globalTimeOffset = time - _time;
            }

            private void PauseControlStation(string id)
            {
                if (!_controlStations.ContainsKey(id))
                {
                    return;
                }
                _controlStations[id].PauseControl();
            }

            private void ResumeControlStation(string id)
            {
                if (!_controlStations.ContainsKey(id))
                {
                    return;
                }
                _controlStations[id].ResumeControl();
            }

            private void QuickLaunch(string controlStationID)
            {
                if (!_controlStations.ContainsKey(controlStationID))
                {
                    return;
                }
                ControlStation controlStation = _controlStations[controlStationID];
                if (!controlStation.HasFireControl || !(controlStation.Controllable is TargetingLaser))
                {
                    return;
                }
                TargetingLaser targetingLaser = controlStation.Controllable as TargetingLaser;
                if (!targetingLaser.HasTarget)
                {
                    return;
                }
                long targetID = targetingLaser.Target.EntityID;
                MissileCoordinator.LaunchMissile(targetID, controlStation);
            }
        }
    }
}
