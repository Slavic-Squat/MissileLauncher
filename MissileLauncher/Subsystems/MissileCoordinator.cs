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
        public class MissileCoordinator
        {
            private Dictionary<string, MissileBay> _missileBays = new Dictionary<string, MissileBay>();
            private HashSet<MissileBay> _selectedBays = new HashSet<MissileBay>();
            private HashSet<long> _registeredAddresses = new HashSet<long>();
            private Dictionary<long, long> _addressTargetIDMap = new Dictionary<long, long>();
            private IReadOnlyDictionary<long, EntityInfoExt> _targetInfo = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, EntityInfoExt> _myMissilesExt = new Dictionary<long, EntityInfoExt>();
            private IEnumerator<int> _launchCoroutine;
            private double _lastClockSync;
            private double _lastLaunch;
            private double _time;

            public IReadOnlyDictionary<long, EntityInfoExt> MyMissilesExt => _myMissilesExt;
            public IReadOnlyDictionary<string, MissileBay> MissileBays => _missileBays;
            public ControlStation Station { get; private set; }
            public bool FireControlAvail => Station == null;
            public int NumBays { get; private set; }
            public int NumSelectedBays => _selectedBays.Count;
            public int NumReadyBays => _missileBays.Count(bay => bay.Value.Status == BayStatus.Ready || bay.Value.Status == BayStatus.Active);
            public bool IsLaunching => _launchCoroutine != null;
            public int NumMissiles => _addressTargetIDMap.Count;

            public MissileCoordinator(int numBays, IReadOnlyDictionary<long, EntityInfoExt> targetInfo)
            {
                NumBays = numBays;
                _targetInfo = targetInfo;
                Init();
            }

            private void Init()
            {
                for (int i = 0; i < NumBays; i++)
                {
                    string id = i.ToString().ToUpper();
                    MissileBay bay = new MissileBay(id);
                    bay.MissileRegistered += () => RegisterMissileAddress(bay.MissileAddress);
                    bay.MissileUnregistered += () => DeselectBay(bay);
                    bay.MissileLaunched += (long targetID) => RegisterMissileTarget(bay.MissileAddress, targetID);

                    if (bay.Status == BayStatus.Ready)
                    {
                        RegisterMissileAddress(bay.MissileAddress);
                    }
                    _missileBays[id] = bay;
                }

                CommunicationHandler0.RegisterTag("MY_MISSILE_INFO", true);
            }

            public void Run(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                foreach (var bay in _missileBays.Values)
                {
                    bay.Run(time);
                    if (!bay.IsSelectable && _selectedBays.Contains(bay))
                    {
                        DeselectBay(bay);
                    }
                }

                while (CommunicationHandler0.HasMessage("MY_MISSILE_INFO", true))
                {
                    MyIGCMessage message;
                    if (CommunicationHandler0.TryRetrieveMessage("MY_MISSILE_INFO", true, out message))
                    {
                        if (!_registeredAddresses.Contains(message.Source))
                        {
                            continue;
                        }
                        byte[] bytes = Convert.FromBase64String(message.Data as string);
                        EntityInfo missile = EntityInfo.Deserialize(bytes, 0);
                        AddMissile(missile);
                    }
                }

                foreach (var missileAddress in _addressTargetIDMap.Keys.ToList())
                {
                    if (!CommunicationHandler0.CanReach(missileAddress))
                    {
                        UnregisterMissile(missileAddress);
                        continue;
                    }                    

                    long targetID = _addressTargetIDMap[missileAddress];
                    if (_targetInfo.ContainsKey(targetID))
                    {
                        byte[] targetBytes = _targetInfo[targetID].Info.Serialize();
                        CommunicationHandler0.SendUnicast(targetBytes, missileAddress, "TARGET_INFO", true);
                    }
                }

                foreach (var missileKey in _myMissilesExt.Keys.ToList())
                {
                    var missile = _myMissilesExt[missileKey];
                    if ((time - missile.TimeRecorded) > 5f)
                    {
                        RemoveMissile(missileKey);
                    }
                }

                if ((time - _lastClockSync) > 10f)
                {
                    SyncClocks();
                }

                if (_launchCoroutine != null && !_launchCoroutine.MoveNext())
                {
                    _launchCoroutine = null;
                }
                _time = time;
            }

            private void AddMissile(EntityInfo entityInfo)
            {
                if (entityInfo.SubType != EntityInfoSubType.MissileInfo) return;

                long key = entityInfo.EntityID;
                long relationID = entityInfo.MissileInfo.Value.LauncherID;
                EntitySource source = EntitySource.Remote;
                EntityRelation relation = EntityRelation.Me;
                EntityInfoExt entityInfoExt = new EntityInfoExt(entityInfo, source, relation, relationID);
                if (!_myMissilesExt.ContainsKey(key))
                {
                    _myMissilesExt.Add(key, entityInfoExt);
                }
                else
                {
                    var original = _myMissilesExt[key];
                    _myMissilesExt[key] = original.Merge(entityInfoExt);
                }
            }

            private void RemoveMissile(long entityID)
            {
                _myMissilesExt.Remove(entityID);
            }

            private void RegisterMissileAddress(long address)
            {
                _registeredAddresses.Add(address);
            }

            private void RegisterMissileTarget(long address, long targetID)
            {
                _addressTargetIDMap[address] = targetID;
            }

            private void UnregisterMissile(long address)
            {
                _registeredAddresses.Remove(address);
                _addressTargetIDMap.Remove(address);
            }

            public void SelectBay(MissileBay bay, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller) || bay == null)
                {
                    return;
                }
                SelectBay(bay);
            }

            private void SelectBay(MissileBay bay)
            {
                if (!bay.IsSelectable) return;
                _selectedBays.Add(bay);
                bay.ActivateMissile();
                bay.IsSelected = true;
            }

            public void DeselectBay(MissileBay bay, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller) || bay == null)
                {
                    return;
                }
                DeselectBay(bay);
            }

            private void DeselectBay(MissileBay bay)
            {
                bay.DeactivateMissile();
                bay.IsSelected = false;
                _selectedBays.Remove(bay);
            }

            public void ToggleBaySelection(MissileBay bay, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller) || bay == null)
                {
                    return;
                }
                ToggleBaySelection(bay);
            }

            private void ToggleBaySelection(MissileBay bay)
            {
                if (_selectedBays.Contains(bay))
                {
                    DeselectBay(bay);
                }
                else
                {
                    SelectBay(bay);
                }
            }

            public void ClearSelectedBays(object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return;
                }
                ClearSelectedBays();
            }

            private void ClearSelectedBays()
            {
                foreach (var bay in _selectedBays.ToList())
                {
                    DeselectBay(bay);
                }
            }

            public void SelectAllBays(object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return;
                }
                SelectAllBays();
            }

            private void SelectAllBays()
            {
                foreach (var bay in _missileBays.Values)
                {
                    SelectBay(bay);
                }
            }

            public void LaunchMissile(long targetID, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller) || IsLaunching)
                {
                    return;
                }
                if (_selectedBays.Count == 0) return;
                var bay = _selectedBays.First();
                LaunchMissile(bay, targetID);
            }

            private void LaunchMissile(MissileBay bay, long targetID)
            {
                if ((_time - _lastLaunch) < 1f || !_selectedBays.Contains(bay)) return;

                bay.Launch(targetID);
                _lastLaunch = _time;
            }

            public void LaunchMissiles(long targetID, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return;
                }
                LaunchMissiles(targetID);
            }

            private void LaunchMissiles(long targetID)
            {
                if (IsLaunching) return;
                _launchCoroutine = HandleLaunch(targetID);
            }

            private IEnumerator<int> HandleLaunch(long targetID)
            {
                int loopCounter = 0;
                foreach (var bay in _selectedBays.ToList())
                {
                    LaunchMissile(bay, targetID);
                    while ((_time - _lastLaunch) < 1f)
                    {
                        yield return loopCounter++;
                    }
                }
                yield return loopCounter;
            }

            private void SyncClocks()
            {
                _lastClockSync = _time;
                double globalTime = SystemCoordinator.GlobalTime;

                foreach (long address in _registeredAddresses)
                {
                    string command = $"SYNC_CLOCK {globalTime}";
                    CommunicationHandler0.SendUnicast(command, address, "COMMANDS", true);
                }
            }

            public void AbortMissile(long address, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return;
                }
                AbortMissile(address);
            }

            private void AbortMissile(long address)
            {
                if (CommunicationHandler0.CanReach(address))
                {
                    string command = "ABORT";
                    CommunicationHandler0.SendUnicast(command, address, "COMMANDS", true);
                }
            }

            public void GiveFireControl(ControlStation station)
            {
                if (station == null || !FireControlAvail || ReferenceEquals(Station, station))
                {
                    return;
                }
                Station = station;
            }

            public void RevokeFireControl(ControlStation station)
            {
                if (station == null || FireControlAvail || !ReferenceEquals(Station, station))
                {
                    return;
                }
                Station = null;
            }

            public string GetOverview()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[MISL COORDINATOR]");
                sb.AppendLine($"  SLCTD BAYS: {NumSelectedBays}/{NumBays}");
                sb.AppendLine($"  RDY BAYS: {NumReadyBays}/{NumBays}");
                sb.AppendLine($"  TRCKD MISLS: {NumMissiles}");
                sb.AppendLine($"  FIRE CTRL: {(FireControlAvail ? "AVAIL" : "IN USE")}");

                return sb.ToString();
            }
        }
    }
}
