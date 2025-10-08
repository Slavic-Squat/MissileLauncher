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
            #region Parts
            private Dictionary<long, MissileInfo> _activeMissiles = new Dictionary<long, MissileInfo>();
            private IMyProgrammableBlock _missileComputer;
            private long _missileID = -1;
            private IMyShipConnector _connector;
            private long _selfID;
            private long _selfAddress;
            #endregion

            #region Properties
            public int ID {  get; private set; }
            public BayStatus Status { get; private set; } = BayStatus.Empty;
            public bool IsSelected { get; set; } = false;
            #endregion

            public MissileBay(int id, long selfID, long selfAddress, Dictionary<long, MissileInfo> activeMissiles)
            {
                ID = id;
                _selfID = selfID;
                _selfAddress = selfAddress;
                _activeMissiles = activeMissiles;

                TryGetBlocks();
                RegisterMissile();
            }

            public bool TryGetBlocks()
            {
                try
                {
                    _connector = GTS.GetBlockWithName($"Missile Bay Connector [{ID}]") as IMyShipConnector;
                    if (_connector == null)
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    return false;
                }
                return true;
            }

            public void RegisterMissile()
            {
                IMyShipConnector missileConnector = _connector.OtherConnector;
                if (missileConnector == null)
                {
                    return;
                }
                List<IMyProgrammableBlock> temp = new List<IMyProgrammableBlock>();
                GTS.GetBlocksOfType(temp, pb => pb.IsSameConstructAs(missileConnector) && pb.CustomName == "Missile Computer");
                _missileComputer = temp.FirstOrDefault();

                if (_missileComputer != null)
                {
                    Status = BayStatus.Loaded;
                    _missileID = _missileComputer.CubeGrid.EntityId;
                }
                else
                {
                    Status = BayStatus.Empty;
                    _missileID = -1;
                }
            }

            public void Run(DateTime time)
            {
                if (!GTS.CanAccess(_missileComputer))
                {
                    Status = BayStatus.Empty;
                    _missileID = -1;
                    return;
                }
            }

            public void InitMissile()
            {
                if (Status == BayStatus.Loaded)
                {
                    _missileComputer.Enabled = true;
                    _missileComputer.CustomData += $"\nInitMissile {_selfAddress} {_selfID}";
                    _missileComputer.TryRun("Init");
                    Status = BayStatus.Ready;
                }
            }

            public void Launch(long targetID)
            {
                if (Status == BayStatus.Ready)
                {
                    _missileComputer.CustomData += $"\nLaunch {targetID}";
                    Status = BayStatus.Launching;
                }
            }

            public void ResetMissile()
            {
                if (Status == BayStatus.Ready)
                {
                    _missileComputer.CustomData += $"\nReset";
                    Status = BayStatus.Loaded;
                    _missileComputer.Enabled = false;
                }
            }

            public override string ToString()
            {
                return $"Bay [{ID}]\n----------------\nSTATUS: {GetName(Status)}\nSELECTED: {IsSelected.ToString().ToUpper()}";
            }
        }
    }
}
