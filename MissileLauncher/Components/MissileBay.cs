using Sandbox;
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
            private double _time;
            private IMyProgrammableBlock _missileComputer;
            private IMyMechanicalConnectionBlock _attachment;
            private bool _isSelected = false;
            private MyIni _missileConfig = new MyIni();
            private string _missileCustomData = "";
            private double _timeLastRegister;

            public string ID {  get; private set; }
            public BayStatus Status { get; private set; } = BayStatus.Empty;
            public MissileType MissileType { get; private set; } = MissileType.Unknown;
            public MissileGuidanceType MissileGuidanceType { get; private set; } = MissileGuidanceType.Unknown;
            public MissilePayload MissilePayload { get; private set; } = MissilePayload.Unknown;
            public MissileStage MissileStage { get; private set; } = MissileStage.Unknown;
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
                    Status = BayStatus.Empty;
                    return;
                }
                List<IMyProgrammableBlock> temp = new List<IMyProgrammableBlock>();
                GTS.GetBlocksOfType(temp, pb => pb.CubeGrid.EntityId == _attachment.TopGrid.EntityId && pb.CustomName.ToUpper().Contains("MISSILE COMPUTER"));
                if (temp.Count == 0)
                {
                    Status = BayStatus.Empty;
                    return;
                }
                _missileComputer = temp[0];
                _missileComputer.Enabled = true;
                if (!_missileComputer.TryRun("INIT"))
                {
                    Status = BayStatus.Empty;
                    return;
                }

                Update();
                
                if (MissileAddress != -1)
                {
                    Status = BayStatus.Building;
                    MissileRegistered?.Invoke();
                }
            }

            private void UnregisterMissile()
            {
                _missileConfig.Clear();
                MissileAddress = -1;
                MissileType = MissileType.Unknown;
                MissileGuidanceType = MissileGuidanceType.Unknown;
                MissilePayload = MissilePayload.Unknown;
                Status = BayStatus.Empty;
                _missileComputer = null;
                MissileUnregistered?.Invoke();
            }

            private void Update()
            {
                if (_missileComputer != null && _missileComputer.CustomData != _missileCustomData)
                {
                    _missileConfig.Clear();
                    if (_missileConfig.TryParse(_missileComputer.CustomData))
                    {
                        MissileAddress = _missileConfig.Get("Config", "MissileAddress").ToInt64(-1);
                        MissileType = MissileEnumHelper.GetMissileType(_missileConfig.Get("Config", "Type").ToString());
                        MissileGuidanceType = MissileEnumHelper.GetMissileGuidanceType(_missileConfig.Get("Config", "GuidanceType").ToString());
                        MissilePayload = MissileEnumHelper.GetMissilePayload(_missileConfig.Get("Config", "Payload").ToString());
                        MissileStage = MissileEnumHelper.GetMissileStage(_missileConfig.Get("Config", "Stage").ToString());
                    }

                    switch (MissileStage)
                    {
                        case MissileStage.Building:
                            Status = BayStatus.Building;
                            break;
                        case MissileStage.Fueling:
                            Status = BayStatus.Fueling;
                            break;
                        case MissileStage.Idle:
                            Status = BayStatus.Ready;
                            break;
                        case MissileStage.Active:
                            Status = BayStatus.Active;
                            break;
                        case MissileStage.Launching:
                            Status = BayStatus.Launching;
                            break;
                    }
                }

                if (Status > BayStatus.Empty && (!_attachment.IsAttached || _missileComputer == null || !GTS.CanAccess(_missileComputer) || MissileAddress == -1))
                {
                    UnregisterMissile();
                }
            }

            public void Run(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                if (Status == BayStatus.Empty && (time - _timeLastRegister) > 5f)
                {
                    RegisterMissile();
                    _timeLastRegister = time;
                }
                else
                {
                    Update();
                }
                _time = time;
            }

            public void ActivateMissile()
            {
                if (Status == BayStatus.Ready)
                {
                    double globalTime = SystemCoordinator.GlobalTime;
                    long selfID = SystemCoordinator.SelfID;
                    if (!_missileComputer.TryRun("TURN_ON")) return;
                    if (!_missileComputer.TryRun($"ACTIVATE {IGCS.Me} {selfID} {globalTime}")) return;
                }
            }

            public void DeactivateMissile()
            {
                if (Status == BayStatus.Active)
                {
                    if (!_missileComputer.TryRun("DEACTIVATE")) return;
                    if (!_missileComputer.TryRun("TURN_OFF")) return;
                }
            }

            public void Launch(long targetID)
            {
                if (Status == BayStatus.Active)
                {
                    if (!_missileComputer.TryRun("LAUNCH")) return;
                    Status = BayStatus.Launching;
                    _attachment.Detach();
                    MissileLaunched?.Invoke(targetID);
                }
            }

            public string GetOverview()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[BAY {ID}]");
                sb.AppendLine($"  STATUS: {MiscEnumHelper.GetBayStatusStr(Status)}");
                sb.AppendLine($"  MISL TYPE: {MissileEnumHelper.GetMissileTypeStr(MissileType)}");
                sb.AppendLine($"  MISL GUIDANCE: {MissileEnumHelper.GetMissileGuidanceStr(MissileGuidanceType)}");
                sb.AppendLine($"  MISL PAYLOAD: {MissileEnumHelper.GetMissilePayloadStr(MissilePayload)}");
                sb.AppendLine($"  MISL STAGE: {MissileEnumHelper.GetMissileStageStr(MissileStage)}");

                return sb.ToString();
            }
        }
    }
}
