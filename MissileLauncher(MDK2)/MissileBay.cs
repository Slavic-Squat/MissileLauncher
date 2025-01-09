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
            private Program program;
            private int ID;
            private int missileCounter;
            private IMyProgrammableBlock missileComputer;

            public enum Status
            {
                Firing, Ready, Fueling, Building, Empty, Exists, Error
            }

            public Status status = Status.Empty;


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

            public void InitMissile(string broadcastTag)
            {
                if (status == Status.Exists)
                {
                    if (missileComputer.TryRun($"InitMissile {broadcastTag} {ID}_{missileCounter} {program.time.Ticks}"))
                    {
                        status = Status.Ready;
                    }
                }
            }

            public void Launch(long targetID)
            {
                if (status == Status.Ready)
                {
                    if (missileComputer.TryRun($"Launch {targetID}"))
                    {
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
