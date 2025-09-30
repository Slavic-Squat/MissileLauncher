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
            private Program _program;
            #endregion

            #region Parts
            private IMyProgrammableBlock _missileComputer;
            private long _selfID;
            private long _selfAddress;
            #endregion

            #region Properties
            public int ID {  get; private set; }      
            public Status State { get; private set; }
            #endregion

            public enum Status
            {
                Empty, Exists, Building, Fueling, Ready, Firing, Error
            }

            public MissileBay(Program program, int id, long selfID, long selfAddress)
            {
                _program = program;
                ID = id;
                _selfID = selfID;
                _selfAddress = selfAddress;

                RegisterMissile();
            }

            public IEnumerator<Status> StateUpdate()
            {
                switch (State)
                {
                    case Status.Firing:

                        while (_program.GridTerminalSystem.CanAccess(_missileComputer))
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
                _missileComputer = _program.GridTerminalSystem.GetBlockWithName($"Missile Computer [{ID}]") as IMyProgrammableBlock;

                if (_missileComputer != null)
                {
                    State = Status.Exists;
                }
            }

            public void InitMissile()
            {
                if (State == Status.Exists)
                {
                    _missileComputer.CustomData = $"InitMissile {_selfAddress} {_selfID}";
                    if (_missileComputer.TryRun("-CommandSent"))
                    {
                        State = Status.Ready;
                    }
                    else
                    {
                        _missileComputer.CustomData = "";
                    }
                }
            }

            public void Launch(long targetID)
            {
                if (State == Status.Ready)
                {
                    _missileComputer.CustomData = $"Launch {targetID}";
                    if (_missileComputer.TryRun("-CommandSent"))
                    {
                        State = Status.Firing;
                    }
                    else
                    {
                        _missileComputer.CustomData = "";
                    }
                }
            }

            public void ResetMissile()
            {

            }
        }
    }
}
