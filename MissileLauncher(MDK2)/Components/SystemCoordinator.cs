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
            public static DateTime SystemTime { get; private set; }
            public static IMyShipController ReferenceController { get; private set; }
            public static Matrix ReferenceBasis => ReferenceController.WorldMatrix;
            public static Vector3 ReferencePosition => ReferenceController.GetPosition();
            public static Vector3 ReferenceVelocity => ReferenceController.GetShipVelocities().LinearVelocity;
            public static long SelfID => ReferenceController.CubeGrid.EntityId;

            public MyIni Config { get; private set; }
            public CommunicationHandler CommunicationHandler { get; private set; }
            public CommandHandler CommandHandler { get; private set; }
            public List<ControlStation> ControlStations { get; private set; }
            public List<TargetingLaser> TargetingLasers { get; private set; }
            public AWACS AWACS { get; private set; }
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public UIWireManager UIWireManager { get; private set; }

            private Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
            private IMyTerminalBlock _storageBlock;

            int _numOfControlStations;
            int _numOfTargetingLasers;

            public SystemCoordinator(int numOfControlStations, int numOfTargetingLasers)
            {
                SystemTime = DateTime.Now;
                _numOfControlStations = numOfControlStations;
                _numOfTargetingLasers = numOfTargetingLasers;
                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                List<IMyShipController> ctrlBlocks = new List<IMyShipController>();
                GTS.GetBlocksOfType(ctrlBlocks, ctrl => ctrl.IsMainCockpit);
                if (ctrlBlocks.Count == 0)
                {
                    throw new Exception("No Main Cockpit Found");
                }
                ReferenceController = ctrlBlocks[0];

                List<IMyTerminalBlock> storageBlocks = new List<IMyTerminalBlock>();
                GTS.GetBlocksOfType(storageBlocks, sb => sb.IsSameConstructAs(MePb) && sb.CustomData.Contains("[Config]"));
                if (storageBlocks.Count == 0)
                {
                    throw new Exception("No Storage Block Found With [Config] In Custom Data");
                }
                _storageBlock = storageBlocks[0];
            }

            private void Init()
            {
                Config = new MyIni();
                if (!Config.TryParse(_storageBlock.CustomData))
                {
                    Config.Clear();
                    Config.Set("Config", "SecureBroadcastPIN", "123456");
                }

                long secureBroadcastPIN = Config.Get("Config", "SecureBroadcastPIN").ToInt64(123456);
                Config.Set("Config", "SecureBroadcastPIN", secureBroadcastPIN.ToString());
                _storageBlock.CustomData = Config.ToString();

                CommandHandler = new CommandHandler(MePb, _commands);
                CommunicationHandler = new CommunicationHandler(0, secureBroadcastPIN);

                ControlStations = new List<ControlStation>();
                TargetingLasers = new List<TargetingLaser>();

                UIWireManager = new UIWireManager(this);

                for (int i = 0; i < _numOfControlStations; i++)
                {
                    ControlStation controlStation = new ControlStation(i, UIWireManager);
                    ControlStations.Add(controlStation);
                }

                for (int i = 0; i < _numOfTargetingLasers; i++)
                {
                    TargetingLaser laser = new TargetingLaser(i);
                    laser.SyncRequested += SyncTarget;
                    TargetingLasers.Add(laser);
                }

                AWACS = new AWACS(0);
                TargetCoordinator = new TargetCoordinator(0, CommunicationHandler);
                MissileCoordinator = new MissileCoordinator(0, 8, CommunicationHandler, TargetCoordinator.AllTargetsExt);
            }

            public void Run()
            {
                SystemTime += RuntimeInfo.TimeSinceLastRun;
                DebugEcho($"System Time: {SystemTime}");
                CommunicationHandler.Recieve();
                CommandHandler.RunCustomDataCommands();

                foreach (var controlStation in ControlStations)
                {
                    controlStation.Run(SystemTime);
                }

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
            }

            public void SyncTarget(TargetingLaser laser)
            {
                EntityInfoExt target = laser.Target;
                DebugEcho("SYNCING");

                if (target.IsValid)
                    AWACS.AddTarget(target);
            }

            public void Command(string command)
            {
                CommandHandler.RunCommands(command);
            }
        }
    }
}
