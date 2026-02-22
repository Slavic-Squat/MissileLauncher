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

            private Dictionary<string, ControlStation> _controlStations = new Dictionary<string, ControlStation>();

            private bool _isMainClock = true;
            private double _lastClockSync;
            private double _globalTimeOffset;
            private double _time;
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public UICoordinator UICoordinator { get; private set; }
            public IReadOnlyDictionary<string, ControlStation> ControlStations => _controlStations;

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
                    throw new Exception($"main controller not found!");
                }
            }

            private void Init()
            {
                _isMainClock = Config.Get("Config", "IsMainClock").ToBoolean(true);
                Config.Set("Config", "IsMainClock", _isMainClock);

                TargetCoordinator = new TargetCoordinator();
                MissileCoordinator = new MissileCoordinator(TargetCoordinator.AllTargets);
                UICoordinator = new UICoordinator(this);

                int numControlStations = Config.Get("Config", "NumControlStations").ToInt32(1);
                Config.Set("Config", "NumControlStations", numControlStations);
                for (int i = 0; i < numControlStations; i++)
                {
                    string id = i.ToString().ToUpper();
                    ControlStation controlStation = new ControlStation(id, UICoordinator);
                    _controlStations[id] = controlStation;
                }

                MePb.CustomData = Config.ToString();

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

                TargetCoordinator.Run(time);
                MissileCoordinator.Run(time);
                UICoordinator.Run();

                foreach (var controlStation in _controlStations.Values)
                {
                    controlStation.Run(time);
                }

                Receive();
                Transmit();

                _time = time;
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
                if (!targetingLaser.TargetSet)
                {
                    return;
                }
                long targetID = targetingLaser.Target.EntityID;
                MissileCoordinator.LaunchMissile(targetID, controlStation);
            }

            private void Transmit()
            {
                if (_isMainClock && (_time - _lastClockSync) > 10f)
                {
                    string command = $"SYNC_CLOCK {_time}";
                    CommunicationHandler0.SendBroadcast(command, "FRIENDLY_COMMANDS", true);
                    _lastClockSync = _time;
                }
            }

            private void Receive()
            {
                while (CommunicationHandler0.HasMessage("FRIENDLY_COMMANDS", true))
                {
                    MyIGCMessage msg;
                    if (CommunicationHandler0.TryRetrieveMessage("FRIENDLY_COMMANDS", true, out msg))
                    {
                        string command = msg.As<string>();
                        CommandHandler0.RunCommands(command);
                    }
                }
            }
        }
    }
}
