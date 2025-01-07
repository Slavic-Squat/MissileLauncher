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
            Program program;
            int ID;
            private List<MissileBay> missileBays = new List<MissileBay>();
            private TargetingLaser targetingLaser;
            private AWACS awacs;
            private TargetCoordinator targetCoordinator;
            private IMyShipController controller;
            private long selectedTarget;
            private int selectedTargetIndex;
            private IMyTextSurface display;

            public MissileLauncher(Program program, int ID, int numberOfMissileBays)
            {
                this.program = program;
                this.ID = ID;

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
                    targetCoordinator = new TargetCoordinator(program, 0, "JombieMissile");
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
            }

            public void LaunchNextAvailableMissile(long targetID)
            {
                int missileBayIndex = missileBays.FindIndex(x => x.status == MissileBay.Status.Ready);
                if (missileBayIndex != -1)
                {
                    missileBays[missileBayIndex].Launch($"Target {targetID} [{ID}]");
                }
            }

            public void LaunchNextAvailableMissile()
            {
                LaunchNextAvailableMissile(selectedTarget);
            }

            public void SyncTarget()
            {
                long targetID = targetingLaser.lockedTarget.EntityId;
                Vector3 targetPosition = targetingLaser.lockedTarget.Position;
                Vector3 targetVelocity = targetingLaser.lockedTarget.Velocity;
                DateTime lastDetectionTime = targetingLaser.lastTargetDetection;

                awacs.AddTarget(targetID, targetPosition, targetVelocity, lastDetectionTime);
            }

            public void UpdateTargetCoordinator()
            {
                targetCoordinator.AddTargets(awacs.lockedTargetsInfo);
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
