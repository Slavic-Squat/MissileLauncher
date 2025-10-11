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
            private bool _isSelected = false;
            #endregion

            #region Properties
            public int ID {  get; private set; }
            public BayStatus Status { get; private set; } = BayStatus.Empty;
            public MissileType MissileType { get; private set; } = MissileType.Unknown;
            public MissileGuidanceType MissileGuidanceType { get; private set; } = MissileGuidanceType.Unknown;
            public MissilePayload MissilePayload { get; private set; } = MissilePayload.Unknown;
            public long MissileID { get; private set; } = -1;
            public long MissileAddress { get; private set; } = -1;
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

            private DateTime _timeLastRegister = DateTime.MinValue;

            public MissileBay(int id)
            {
                ID = id;

                TryGetBlocks();
            }

            private bool TryGetBlocks()
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
                    DebugEcho($"Error: Unable to find Missile Bay Connector [{ID}]");
                    return false;
                    throw;
                }
                return true;
            }

            private void RegisterMissile()
            {
                MissileID = -1;
                MissileAddress = -1;
                MissileType = MissileType.Unknown;
                MissileGuidanceType = MissileGuidanceType.Unknown;
                MissilePayload = MissilePayload.Unknown;
                Status = BayStatus.Empty;
                _missileComputer = null;

                IMyShipConnector missileConnector = _connector.OtherConnector;
                if (missileConnector == null)
                {
                    return;
                }
                List<IMyProgrammableBlock> pbBlocks = new List<IMyProgrammableBlock>();
                GTS.GetBlocksOfType(pbBlocks, pb => pb.IsSameConstructAs(missileConnector) && pb.CustomName == "Computer");
                if (pbBlocks.Count == 0)
                {
                    return;
                }
                _missileComputer = pbBlocks[0];

                List<IMyTerminalBlock> tBlocks = new List<IMyTerminalBlock>();
                GTS.GetBlocksOfType(tBlocks, b => b.IsSameConstructAs(missileConnector) && b.CustomData.Contains("[Data]"));
                if (tBlocks.Count == 0)
                {
                    return;
                }
                IMyTerminalBlock storageBlock = tBlocks[0];

                MyIni missileConfig = new MyIni();
                if (missileConfig.TryParse(storageBlock.CustomData))
                {
                    MissileID = missileConfig.Get("Data", "ID").ToInt64(-1);
                    MissileAddress = missileConfig.Get("Data", "Address").ToInt64(-1);
                    MissileType type;
                    Enum.TryParse(missileConfig.Get("Data", "Type").ToString(), out type);
                    MissileType = type;
                    MissileGuidanceType guidanceType;
                    Enum.TryParse(missileConfig.Get("Data", "GuidanceType").ToString(), out guidanceType);
                    MissileGuidanceType = guidanceType;
                    MissilePayload payload;
                    Enum.TryParse(missileConfig.Get("Data", "Payload").ToString(), out payload);
                    MissilePayload = payload;
                }
                else
                {
                    return;
                }
                
                if (MissileID != -1 && MissileAddress != -1 && MissileType != MissileType.Unknown && MissileGuidanceType != MissileGuidanceType.Unknown && MissilePayload != MissilePayload.Unknown)
                {
                    Status = BayStatus.Loaded;
                }
            }

            public void Run(DateTime time)
            {
                if (Status == BayStatus.Empty && time - _timeLastRegister > TimeSpan.FromSeconds(10))
                {
                    RegisterMissile();
                    _timeLastRegister = time;
                }
                if (_missileComputer != null && !GTS.CanAccess(_missileComputer))
                {
                    Status = BayStatus.Empty;
                    MissileID = -1;
                    MissileAddress = -1;
                    MissileType = MissileType.Unknown;
                    MissileGuidanceType = MissileGuidanceType.Unknown;
                    MissilePayload = MissilePayload.Unknown;
                    _missileComputer = null;
                    return;
                }
            }

            public bool TryInitMissile(DateTime time)
            {
                if (Status == BayStatus.Loaded)
                {
                    _missileComputer.Enabled = true;
                    if (!_missileComputer.TryRun("ON"))
                    {
                        return false;
                    }
                    _missileComputer.CustomData += $"\nInit {SystemCoordinator.SelfAddress} {time}";                    
                    Status = BayStatus.Ready;
                    return true;
                }
                return false;
            }

            public bool Launch()
            {
                if (Status == BayStatus.Ready)
                {
                    _missileComputer.CustomData += $"\nLaunch";
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
                return $"Bay [{ID}]\n----------------\nSTATUS: {GetName(Status)}\nMISL TYPE: {GetName(MissileType)}\nMISL GUIDANCE: {MissileGuidanceType}\nMISL PAYLOAD: {GetName(MissilePayload)}\n";
            }
        }
    }
}
