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
            #region Fields
            private int _missileCounter;
            #endregion

            #region Parts
            private IMyProgrammableBlock _missileComputer;
            #endregion

            #region Properties
            public Program Program { get; private set; }
            public int ID {  get; private set; }      
            public Status State { get; private set; }
            #endregion

            public enum Status
            {
                Empty, Exists, Building, Fueling, Ready, Firing, Error
            }

            public MissileBay(Program Program, int ID)
            {
                this.Program = Program;
                this.ID = ID;

                RegisterMissile();
            }

            public IEnumerator<Status> StateUpdate()
            {
                switch (State)
                {
                    case Status.Firing:

                        while (Program.GridTerminalSystem.CanAccess(_missileComputer))
                        {
                            yield return Status.Firing;
                        }
                        State = Status.Empty;
                        yield return Status.Empty;
                        break;
                }
            }

            public void RegisterMissile()
            {
                _missileComputer = Program.GridTerminalSystem.GetBlockWithName($"Missile Computer [{ID}]") as IMyProgrammableBlock;

                if (_missileComputer != null)
                {
                    State = Status.Exists;
                }
            }

            public void InitMissile(string launcherTag)
            {
                if (State == Status.Exists)
                {
                    if (ConfigUtilties.TryQueueExternalCommand(_missileComputer, $"InitMissile {launcherTag} {ID}_{_missileCounter} {Program.time.Ticks}"))
                    {
                        _missileComputer.TryRun("-ConfigUpdated");
                        State = Status.Ready;
                    }
                }
            }

            public void Launch(long targetID)
            {
                if (State == Status.Ready)
                {
                    if (ConfigUtilties.TryQueueExternalCommand(_missileComputer, $"Launch {targetID}"))
                    {
                        _missileComputer.TryRun("-ConfigUpdated");
                        State = Status.Firing;
                    }
                }
            }

            public void ResetMissile()
            {

            }
        }
    }
}
