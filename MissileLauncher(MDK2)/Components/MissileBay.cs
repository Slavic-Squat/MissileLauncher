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
            public double Time { get; private set; }
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

            private double _timeLastRegister;

            public MissileBay(int id)
            {
                ID = id;

                GetBlocks();
            }

            private void GetBlocks()
            {
                _connector = AllGridBlocks.Find(b => b is IMyShipConnector && b.CustomName.Contains($"Missile Bay {ID}")) as IMyShipConnector;
                if (_connector == null)
                {
                    DebugWrite($"Error: No connector found for Missile Bay {ID}!\n", true);
                    throw new Exception($"No connector found for Missile Bay {ID}!\n");
                }
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

                if (_connector.Status != MyShipConnectorStatus.Connected)
                {
                    return;
                }
                IMyShipConnector missileConnector = _connector.OtherConnector;
                List<IMyProgrammableBlock> pbBlocks = new List<IMyProgrammableBlock>();
                GTS.GetBlocksOfType(pbBlocks, pb => pb.IsSameConstructAs(missileConnector) && pb.CustomName.Contains("Missile Computer"));
                if (pbBlocks.Count == 0)
                {
                    return;
                }
                _missileComputer = pbBlocks[0];

                MyIni missileConfig = new MyIni();
                if (missileConfig.TryParse(_missileComputer.CustomData))
                {
                    MissileID = missileConfig.Get("Config", "MissileID").ToInt64(-1);
                    MissileAddress = missileConfig.Get("Config", "MissileAddress").ToInt64(-1);
                    MissileType = GetMissileType(missileConfig.Get("Config", "Type").ToString());
                    MissileGuidanceType = GetMissileGuidanceType(missileConfig.Get("Config", "GuidanceType").ToString());
                    MissilePayload = GetMissilePayload(missileConfig.Get("Config", "Payload").ToString());
                }
                else
                {
                    return;
                }
                
                if (MissileID != -1 && MissileAddress != -1 && MissileType != MissileType.Unknown && MissileGuidanceType != MissileGuidanceType.Unknown && MissilePayload != MissilePayload.Unknown)
                {
                    Status = BayStatus.Ready;
                }
            }

            public void Run(double time)
            {
                if (Status == BayStatus.Empty && (time - _timeLastRegister) > 10f)
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
                }
                Time = time;
            }

            public void Launch()
            {
                if (Status == BayStatus.Ready)
                {
                    double globalTime = SystemCoordinator.GlobalTime;
                    _missileComputer.Enabled = true;
                    if (!_missileComputer.TryRun($"ON | ACTIVATE {IGCS.Me} {globalTime} | LAUNCH"))
                    {
                        return;
                    }
                    Status = BayStatus.Launching;
                }
            }

            public void ResetMissile()
            {
                if (Status == BayStatus.Ready)
                {
                    _missileComputer.TryRun("RESET");
                    Status = BayStatus.Loaded;
                    _missileComputer.Enabled = false;
                }
            }

            public override string ToString()
            {
                return $"Bay [{ID}]\n----------------\nSTATUS: {GetName(Status)}\nMISL TYPE: {GetName(MissileType)}\nMISL GUIDANCE: {GetName(MissileGuidanceType)}\nMISL PAYLOAD: {GetName(MissilePayload)}\n";
            }
        }
    }
}
