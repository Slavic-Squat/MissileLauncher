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
            private IMyProgrammableBlock _missileComputer;
            private IMyShipConnector _connector;
            private MyIni _missileConfig = new MyIni();
            private long _selfID;
            private long _selfAddress;
            private bool _isSelected = false;
            #endregion

            #region Properties
            public int ID {  get; private set; }
            public BayStatus Status { get; private set; } = BayStatus.Empty;
            public MissileType MissileType { get; private set; } = MissileType.Unknown;
            public MissilePayload MissilePayload { get; private set; } = MissilePayload.Unknown;
            public long MissileID { get; private set; } = -1;
            public bool IsSelected
            {
                get
                {
                    return _isSelected;
                }
                set
                {
                    _isSelected = IsSelectable && value;
                }
            }
            public bool IsSelectable => Status == BayStatus.Loaded || Status == BayStatus.Ready;
            #endregion

            public MissileBay(int id, long selfID, long selfAddress)
            {
                ID = id;
                _selfID = selfID;
                _selfAddress = selfAddress;

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

                if (_missileConfig.TryParse(missileConnector.CustomData))
                {
                    byte missileType = _missileConfig.Get("Data", "Type").ToByte();
                    byte missilePayload = _missileConfig.Get("Data", "Payload").ToByte();

                    MissileType = (MissileType)missileType;
                    MissilePayload = (MissilePayload)missilePayload;
                }
                if (_missileComputer != null)
                {
                    Status = BayStatus.Loaded;
                    MissileID = _missileComputer.CubeGrid.EntityId;
                }
                else
                {
                    Status = BayStatus.Empty;
                    MissileID = -1;
                }
            }

            public void Run(DateTime time)
            {
                if (!GTS.CanAccess(_missileComputer))
                {
                    Status = BayStatus.Empty;
                    MissileID = -1;
                    _missileConfig.Clear();
                    MissileType = MissileType.Unknown;
                    MissilePayload = MissilePayload.Unknown;
                    return;
                }
            }

            public bool InitMissile()
            {
                if (Status == BayStatus.Loaded)
                {
                    _missileComputer.Enabled = true;
                    _missileComputer.CustomData += $"\nInitMissile {_selfAddress} {_selfID}";
                    if (!_missileComputer.TryRun("Init"))
                    {
                        _missileComputer.CustomData = "";
                        return false;
                    }
                    Status = BayStatus.Ready;
                    return true;
                }
                return false;
            }

            public bool Launch(long targetID)
            {
                if (Status == BayStatus.Ready)
                {
                    _missileComputer.CustomData += $"\nLaunch {targetID}";
                    Status = BayStatus.Launching;
                    return true;
                }
                return false;
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
                return $"Bay [{ID}]\n----------------\nSTATUS: {GetName(Status)}\nMISL TYPE: {GetName(MissileType)}\nMISL PAYLOAD: {GetName(MissilePayload)}\n";
            }
        }
    }
}
