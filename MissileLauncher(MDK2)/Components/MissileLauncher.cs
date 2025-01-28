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
        public class MissileLauncher : IMissileLauncher
        {
            #region Parts
            private IMyShipController _controller;
            private IMyTextSurface _display;
            #endregion

            #region State Info
            private long _selectedTarget;
            private int _selectedTargetIndex;
            #endregion

            #region Properties
            public Program Program { get; private set; }
            public int ID { get; private set; }
            public string Name { get; private set; }
            public string LauncherTag { get; private set; }
            #endregion

            #region Components
            public List<MissileBay> MissileBays { get; private set; }
            public TargetingLaser TargetingLaser { get; private set; }
            public AWACS AWACS { get; private set; }
            public TargetCoordinator TargetCoordinator { get; private set; }
            public TargetingSpriteBuilder TargetingSpriteBuilder { get; private set; }
            public TargetingUI TargetingUI { get; private set; }
            #endregion

            public MissileLauncher(Program program, int id, string name, string launcherTag, int numberOfMissileBays)
            {
                Program = program;
                ID = id;
                Name = name;
                LauncherTag = launcherTag;

                TryGetBlocks();

                MissileBays = new List<MissileBay>();
                for (int i = 0; i < numberOfMissileBays; i++)
                {
                    MissileBays.Add(new MissileBay(Program, i));
                }
                TargetingLaser = new TargetingLaser(Program, 0, _controller, maxRaycastDistance: 10000);
                AWACS = new AWACS(Program, 0, maxRaycastDistance: 10000);
                TargetCoordinator = new TargetCoordinator(Program, 0, _controller, Name, LauncherTag);
                TargetingSpriteBuilder = new TargetingSpriteBuilder(_controller, 30, 1, 100, 100000);
                TargetingSpriteBuilder.Targets = TargetCoordinator.Targets;
                TargetingSpriteBuilder.Missiles = TargetCoordinator.Missiles;
                TargetingUI = new TargetingUI(_display, TargetingSpriteBuilder);
            }

            public bool TryGetBlocks()
            {
                try
                {
                    _controller = Program.GridTerminalSystem.GetBlockWithName($"Launch Controller [{ID}]") as IMyShipController;
                    if (_controller == null)
                    {
                        throw new Exception();
                    }
                    _display = Program.GridTerminalSystem.GetBlockWithName($"Launch Display [{ID}]") as IMyTextSurface;
                    if (_display == null)
                    {
                        throw new Exception();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Program.Echo("Error in MissileLauncher Construction");
                    return false;
                }
            }

            public void Run(DateTime time)
            {
                TargetingLaser.Run(time);
                AWACS.Run(time);
                TargetCoordinator.Run(time);
                UpdateTargetCoordinator();
                TargetingSpriteBuilder.Run(time);
                TargetingUI.Run(time);
            }

            public void InitNextAvailableMissile()
            {
                MissileBay missileBay = MissileBays.Find(x => x.State == MissileBay.Status.Exists);
                missileBay?.InitMissile(LauncherTag);
            }

            public void LaunchNextAvailableMissile(long targetID)
            {
                MissileBay missileBay = MissileBays.Find(x => x.State == MissileBay.Status.Ready);
                missileBay?.Launch(targetID);
            }

            public void LaunchNextAvailableMissile()
            {
                LaunchNextAvailableMissile(_selectedTarget);
            }

            public void SyncTarget()
            {
                AWACS.AddTarget(TargetingLaser.Target);
            }

            public void UpdateTargetCoordinator()
            {
                TargetCoordinator.AddTargets(AWACS.Targets);
                TargetCoordinator.AddTarget(TargetingLaser.Target);
            }

            public void SelectNextTarget()
            {
                if (TargetCoordinator.TargetIDs.Count != 0)
                {
                    _selectedTargetIndex = TargetCoordinator.TargetIDs.IndexOf(_selectedTarget);
                    if (_selectedTargetIndex == -1)
                    {
                        _selectedTargetIndex = 0;
                    }
                    else
                    {
                        _selectedTargetIndex++;
                        _selectedTargetIndex %= TargetCoordinator.TargetIDs.Count;
                    }

                    _selectedTarget = TargetCoordinator.TargetIDs[_selectedTargetIndex];
                }
            }

            public void SelectPreviousTarget()
            {
                if (TargetCoordinator.TargetIDs.Count != 0)
                {
                    _selectedTargetIndex = TargetCoordinator.TargetIDs.IndexOf(_selectedTarget);
                    if (_selectedTargetIndex == -1)
                    {
                        _selectedTargetIndex = 0;
                    }
                    else
                    {
                        _selectedTargetIndex--;
                        _selectedTargetIndex %= TargetCoordinator.TargetIDs.Count;
                    }

                    _selectedTarget = TargetCoordinator.TargetIDs[_selectedTargetIndex];
                }
            }
        }
    }
}
