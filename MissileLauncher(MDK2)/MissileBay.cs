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
        public class MissileBay
        {
            #region General Info
            private Program program;
            private int ID;
            #endregion

            #region Parts
            private IMyProgrammableBlock missileComputer;
            #endregion

            #region State Info
            private int missileCounter;
            public Status status = Status.Empty;
            #endregion

            public enum Status
            {
                Firing, Ready, Fueling, Building, Empty, Exists, Error
            }

            public MissileBay(Program program, int ID)
            {
                this.program = program;
                this.ID = ID;

                RegisterMissile();
            }

            public IEnumerator<Status> State()
            {
                switch (status)
                {
                    case Status.Firing:

                        while (program.GridTerminalSystem.CanAccess(missileComputer))
                        {
                            yield return Status.Firing;
                        }
                        status = Status.Empty;
                        yield return Status.Empty;
                        break;
                }
            }

            public void RegisterMissile()
            {
                missileComputer = program.GridTerminalSystem.GetBlockWithName($"Missile Computer [{ID}]") as IMyProgrammableBlock;

                if (missileComputer != null)
                {
                    status = Status.Exists;
                }
            }

            public void InitMissile(string launcherTag)
            {
                if (status == Status.Exists)
                {
                    if (ConfigUtilties.TryQueueExternalCommand(missileComputer, $"InitMissile {launcherTag} {ID}_{missileCounter} {program.time.Ticks}"))
                    {
                        missileComputer.TryRun("-ConfigUpdated");
                        status = Status.Ready;
                    }
                }
            }

            public void Launch(long targetID)
            {
                if (status == Status.Ready)
                {
                    if (ConfigUtilties.TryQueueExternalCommand(missileComputer, $"Launch {targetID}"))
                    {
                        missileComputer.TryRun("-ConfigUpdated");
                        status = Status.Firing;
                    }
                }
            }

            public void ResetMissile()
            {

            }
        }
    }
}
