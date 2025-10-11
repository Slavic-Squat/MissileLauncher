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
            public static Matrix ReferenceBasis => _referenceController.WorldMatrix;
            public static Vector3 ReferencePosition => _referenceController.GetPosition();
            public static Vector3 ReferenceVelocity => _referenceController.GetShipVelocities().LinearVelocity;
            public static DateTime SystemTime { get; private set; }
            public static long SelfID => _referenceController.CubeGrid.EntityId;
            public static long SelfAddress => IGCS.Me;

            public MyIni Conifg = new MyIni();
            public CommunicationHandler CommunicationHandler { get; private set; }
            public CommandHandler CommandHandler { get; private set; }
            public List<ControlStation> ControlStations { get; private set; }
            public List<TargetingLaser> TargetingLasers { get; private set; }
            public AWACS AWACS { get; private set; }
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public UIWireManager UIWireManager { get; private set; }

            private List<IEnumerator<bool>> _coroutines = new List<IEnumerator<bool>>();
            private Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
            private static IMyShipController _referenceController;
            private IMyTerminalBlock _storageBlock;

            public SystemCoordinator(int numOfControlStations, int numOfTargetingLasers)
            {
                TryGetBlocks();
                ControlStations = new List<ControlStation>();
                TargetingLasers = new List<TargetingLaser>();

                UIWireManager = new UIWireManager(this);

                for (int i = 0; i < numOfControlStations; i++)
                {
                    ControlStation controlStation = new ControlStation(i, UIWireManager);
                    ControlStations.Add(controlStation);
                }

                for (int i = 0; i < numOfTargetingLasers;  i++)
                {
                    TargetingLaser laser = new TargetingLaser(i);
                    laser.SyncRequested += SyncTarget;
                    TargetingLasers.Add(laser);
                }

                CommunicationHandler = new CommunicationHandler(0);
                AWACS = new AWACS(0);
                TargetCoordinator = new TargetCoordinator(0, CommunicationHandler);
                MissileCoordinator = new MissileCoordinator(0, 8, CommunicationHandler, TargetCoordinator.AllTargetsExt);
                CommandHandler = new CommandHandler(MePb, _commands);
            }

            public bool TryGetBlocks()
            {
                try
                {
                    List<IMyShipController> ctrlBlocks = new List<IMyShipController>();
                    GTS.GetBlocksOfType(ctrlBlocks, ctrl => ctrl.IsMainCockpit);
                    _referenceController = ctrlBlocks.Count > 0 ? ctrlBlocks[0] : null;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public void Run()
            {
                SystemTime += RuntimeInfo.TimeSinceLastRun;
                DebugEcho($"System Time: {SystemTime}");
                CommunicationHandler.Recieve();
                CommandHandler.RunCustomDataCommands();

                for (int i = _coroutines.Count - 1; i >= 0; i--)
                {
                    var coroutine = _coroutines[i];
                    if (coroutine.MoveNext())
                    {
                        _coroutines.RemoveAt(i);
                    }
                }

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
                CommandHandler.TryRunCommands(command);
            }
        }
    }
}
