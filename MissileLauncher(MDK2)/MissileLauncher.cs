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
        public class MissileLauncher
        {
            #region General Info
            Program program;
            int ID;
            string name;
            #endregion

            #region Broadcast Info
            string launcherTag;
            #endregion

            #region Parts
            private IMyShipController controller;
            private IMyTextSurface display;
            #endregion

            #region State Info
            private long selectedTarget;
            private int selectedTargetIndex;
            #endregion

            #region Components
            private List<MissileBay> missileBays = new List<MissileBay>();
            private TargetingLaser targetingLaser;
            private AWACS awacs;
            private TargetCoordinator targetCoordinator;
            private TargetingUI targetingUI;
            #endregion

            public MissileLauncher(Program program, int ID, string name, string launcherTag, int numberOfMissileBays)
            {
                this.program = program;
                this.ID = ID;
                this.name = name;
                this.launcherTag = launcherTag;

                try
                {
                    controller = program.GridTerminalSystem.GetBlockWithName($"Launch Controller [{ID}]") as IMyShipController;
                    if (controller == null)
                    {
                        throw new Exception();
                    }
                    display = program.GridTerminalSystem.GetBlockWithName($"Launch Display [{ID}]") as IMyTextSurface;
                    if (display == null)
                    {
                        throw new Exception();
                    }
                    for (int i = 0; i < numberOfMissileBays; i++)
                    {
                        missileBays.Add(new MissileBay(program, i));
                    }
                    targetingLaser = new TargetingLaser(program, 0, controller);
                    awacs = new AWACS(program, 0);
                    targetCoordinator = new TargetCoordinator(program, 0, controller, name, launcherTag);
                    targetingUI = new TargetingUI(program, 0, display, controller);
                }
                catch (Exception ex)
                {
                    program.Echo("Error in MissileLauncher Construction");
                    throw;
                }
            }

            public void Run(DateTime time)
            {
                targetingLaser.Run(time);
                awacs.Run(time);
                targetCoordinator.Run(time);
                UpdateTargetCoordinator();
                targetingUI.selectedTarget = selectedTarget;
                targetingUI.AddTargets(targetCoordinator.targetsInfo);
                targetingUI.Run(time);
            }

            public void InitNextAvailableMissile()
            {
                MissileBay missileBay = missileBays.Find(x => x.status == MissileBay.Status.Exists);
                missileBay?.InitMissile(launcherTag);
            }

            public void LaunchNextAvailableMissile(long targetID)
            {
                MissileBay missileBay = missileBays.Find(x => x.status == MissileBay.Status.Ready);
                missileBay?.Launch(targetID);
            }

            public void LaunchNextAvailableMissile()
            {
                LaunchNextAvailableMissile(selectedTarget);
            }

            public void SyncTarget()
            {
                awacs.AddTarget(targetingLaser.lockedTarget.EntityId, targetingLaser.lockedTarget.Position, targetingLaser.lockedTarget.Velocity, targetingLaser.lastTargetDetection);
            }

            public void UpdateTargetCoordinator()
            {
                targetCoordinator.AddTargets(awacs.lockedTargetsInfo);
                targetCoordinator.AddTarget(targetingLaser.lockedTarget.EntityId, targetingLaser.lockedTarget.Position, targetingLaser.lockedTarget.Velocity, targetingLaser.lastTargetDetection);
            }

            public void SelectNextTarget()
            {
                if (targetCoordinator.targetIDs.Count != 0)
                {
                    selectedTargetIndex = targetCoordinator.targetIDs.IndexOf(selectedTarget);
                    if (selectedTargetIndex == -1)
                    {
                        selectedTargetIndex = 0;
                    }
                    else
                    {
                        selectedTargetIndex++;
                        selectedTargetIndex %= targetCoordinator.targetIDs.Count;
                    }

                    selectedTarget = targetCoordinator.targetIDs[selectedTargetIndex];
                }
            }

            public void SelectPreviousTarget()
            {
                if (targetCoordinator.targetIDs.Count != 0)
                {
                    selectedTargetIndex = targetCoordinator.targetIDs.IndexOf(selectedTarget);
                    if (selectedTargetIndex == -1)
                    {
                        selectedTargetIndex = 0;
                    }
                    else
                    {
                        selectedTargetIndex--;
                        selectedTargetIndex %= targetCoordinator.targetIDs.Count;
                    }

                    selectedTarget = targetCoordinator.targetIDs[selectedTargetIndex];
                }
            }
        }
    }
}
