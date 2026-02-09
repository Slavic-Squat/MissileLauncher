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
            private IMyProgrammableBlock _missileComputer;
            private IMyMechanicalConnectionBlock _attachment;
            private bool _isSelected = false;

            public string ID {  get; private set; }
            public double Time { get; private set; }
            public BayStatus Status { get; private set; } = BayStatus.Empty;
            public MissileType MissileType { get; private set; } = MissileType.Unknown;
            public MissileGuidanceType MissileGuidanceType { get; private set; } = MissileGuidanceType.Unknown;
            public MissilePayload MissilePayload { get; private set; } = MissilePayload.Unknown;
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
            public bool IsSelectable => Status == BayStatus.Ready || Status == BayStatus.Active;

            private double _timeLastRegister;

            public event Action MissileRegistered;
            public event Action MissileUnregistered;
            public event Action<long> MissileLaunched;

            public MissileBay(string id)
            {
                ID = id.ToUpper();

                GetBlocks();
            }

            private void GetBlocks()
            {
                _attachment = AllGridBlocks.Where(b => b is IMyMechanicalConnectionBlock && b.CustomName.ToUpper().Contains($"MISSILE BAY {ID} ATTACHMENT")).FirstOrDefault() as IMyMechanicalConnectionBlock;
                if (_attachment == null)
                {
                    DebugWrite($"Error: No attachment found for Missile Bay {ID}!\n", true);
                    throw new Exception($"No attachment found for Missile Bay {ID}!\n");
                }
            }

            private void RegisterMissile()
            {
                UnregisterMissile();

                if (_attachment.TopGrid == null)
                {
                    return;
                }
                List<IMyProgrammableBlock> temp = new List<IMyProgrammableBlock>();
                GTS.GetBlocksOfType(temp, pb => pb.CubeGrid.EntityId == _attachment.TopGrid.EntityId && pb.CustomName.ToUpper().Contains("MISSILE COMPUTER"));
                if (temp.Count == 0)
                {
                    return;
                }
                _missileComputer = temp[0];

                MyIni missileConfig = new MyIni();
                bool missileReady = false;
                if (missileConfig.TryParse(_missileComputer.CustomData))
                {
                    MissileAddress = missileConfig.Get("Config", "MissileAddress").ToInt64(-1);
                    MissileType = MissileEnumHelper.GetMissileType(missileConfig.Get("Config", "Type").ToString());
                    MissileGuidanceType = MissileEnumHelper.GetMissileGuidanceType(missileConfig.Get("Config", "GuidanceType").ToString());
                    MissilePayload = MissileEnumHelper.GetMissilePayload(missileConfig.Get("Config", "Payload").ToString());
                    missileReady = missileConfig.Get("Config", "Ready").ToBoolean(false);
                }
                else
                {
                    return;
                }
                
                if (MissileAddress != -1 && MissileType != MissileType.Unknown && MissileGuidanceType != MissileGuidanceType.Unknown && MissilePayload != MissilePayload.Unknown && missileReady)
                {
                    Status = BayStatus.Ready;
                    MissileRegistered?.Invoke();
                }
            }

            private void UnregisterMissile()
            {
                MissileAddress = -1;
                MissileType = MissileType.Unknown;
                MissileGuidanceType = MissileGuidanceType.Unknown;
                MissilePayload = MissilePayload.Unknown;
                Status = BayStatus.Empty;
                _missileComputer = null;
                MissileUnregistered?.Invoke();
            }

            public void Run(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }

                if (Status == BayStatus.Empty && (time - _timeLastRegister) > 10f)
                {
                    RegisterMissile();
                    _timeLastRegister = time;
                }
                if (_missileComputer != null && !GTS.CanAccess(_missileComputer))
                {
                    UnregisterMissile();
                }
                Time = time;
            }

            public void ActivateMissile()
            {
                if (Status == BayStatus.Ready)
                {
                    double globalTime = SystemCoordinator.GlobalTime;
                    _missileComputer.Enabled = true;
                    if (!_missileComputer.TryRun("ON")) return;
                    if (!_missileComputer.TryRun($"ACTIVATE {IGCS.Me} {SystemCoordinator.SelfID} {globalTime}")) return;
                    Status = BayStatus.Active;
                }
            }

            public void DeactivateMissile()
            {
                if (Status == BayStatus.Active)
                {
                    if (!_missileComputer.TryRun("DEACTIVATE")) return;
                    if (!_missileComputer.TryRun("OFF")) return;
                    Status = BayStatus.Ready;
                }
            }

            public void Launch(long targetID)
            {
                if (Status == BayStatus.Active)
                {
                    if (!_missileComputer.TryRun("LAUNCH")) return;
                    Status = BayStatus.Launching;
                    MissileLaunched?.Invoke(targetID);
                }
            }

            public string GetOverview()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[BAY {ID}]");
                sb.AppendLine($"  STATUS: {MiscEnumHelper.GetDisplayString(Status)}");
                sb.AppendLine($"  MISL TYPE: {MissileEnumHelper.GetDisplayString(MissileType)}");
                sb.AppendLine($"  MISL GUIDANCE: {MissileEnumHelper.GetDisplayString(MissileGuidanceType)}");
                sb.AppendLine($"  MISL PAYLOAD: {MissileEnumHelper.GetDisplayString(MissilePayload)}");

                return sb.ToString();
            }
        }
    }
}
