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
            public double Time { get; private set; }
            public Dictionary<long, EntityInfoExt> MyMissilesExt { get; private set; }
            public ControlStation Station { get; private set; }
            public bool FireControlAvail => Station == null;
            public int NumBays { get; private set; }
            public int NumSelectedBays => _selectedBays.Count;
            public int NumReadyBays => MissileBays.Count(bay => bay.Status == BayStatus.Ready || bay.Status == BayStatus.Active);
            public bool IsLaunching => _launchCoroutine != null;
            public int NumMissiles => _addressTargetIDMap.Count;
            public List<MissileBay> MissileBays { get; private set; }
            private HashSet<MissileBay> _selectedBays = new HashSet<MissileBay>();
            private Dictionary<long, long> _addressMissileIDMap = new Dictionary<long, long>();
            private Dictionary<long, long> _addressTargetIDMap = new Dictionary<long, long>();
            private Dictionary<long, long> _missileIDAddressMap = new Dictionary<long, long>();

            private Dictionary<long, EntityInfoExt> _targetInfo = new Dictionary<long, EntityInfoExt>();

            private IEnumerator<int> _launchCoroutine;

            private double _lastClockSync;
            private double _lastLaunch;

            public MissileCoordinator(int numBays, Dictionary<long, EntityInfoExt> targetInfo)
            {
                NumBays = numBays;
                _targetInfo = targetInfo;
                Init();
            }

            private void Init()
            {
                MissileBays = new List<MissileBay>();
                MyMissilesExt = new Dictionary<long, EntityInfoExt>();
                for (int i = 0; i < NumBays; i++)
                {
                    MissileBay bay = new MissileBay(i);
                    bay.MissileRegistered += () => RegisterMissileAddress(bay.MissileAddress, bay.MissileID);
                    bay.MissileUnregistered += () => DeselectBay(bay);
                    bay.MissileLaunched += (long targetID) => RegisterMissileTarget(bay.MissileAddress, targetID);
                    MissileBays.Add(bay);

                }

                CommunicationHandler0.RegisterTag("MyMissileInfo", true);
            }

            public void Run(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }

                foreach (var bay in MissileBays)
                {
                    bay.Run(time);
                }

                while (CommunicationHandler0.HasMessage("MyMissileInfo", true))
                {
                    MyIGCMessage message;
                    if (CommunicationHandler0.TryRetrieveMessage("MyMissileInfo", true, out message))
                    {
                        if (!_addressMissileIDMap.ContainsKey(message.Source))
                        {
                            continue;
                        }
                        byte[] bytes = Convert.FromBase64String(message.Data as string);
                        EntityInfo missile = EntityInfo.Deserialize(bytes, 0);
                        AddMissile(missile);
                    }
                }

                foreach (var missileAddress in _addressMissileIDMap.Keys.ToList())
                {
                    if (!CommunicationHandler0.CanReach(missileAddress))
                    {
                        long missileID = _addressMissileIDMap[missileAddress];
                        RemoveMissile(missileID);
                        UnregisterMissile(missileAddress);
                        continue;
                    }

                    byte[] selfBytes = SystemCoordinator.SelfInfo.Serialize();
                    CommunicationHandler0.SendUnicast(selfBytes, missileAddress, "LauncherInfo", true);
                }

                foreach (var missileAddress in _addressTargetIDMap.Keys.ToList())
                {
                    if (!CommunicationHandler0.CanReach(missileAddress))
                    {
                        long missileID = _addressMissileIDMap[missileAddress];
                        RemoveMissile(missileID);
                        UnregisterMissile(missileAddress);
                        continue;
                    }                    

                    long targetID = _addressTargetIDMap[missileAddress];
                    if (_targetInfo.ContainsKey(targetID))
                    {
                        byte[] targetBytes = _targetInfo[targetID].Info.Serialize();
                        CommunicationHandler0.SendUnicast(targetBytes, missileAddress, "TargetInfo", true);
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
                Time = time;
            }

            private void AddMissile(EntityInfo entityInfo)
            {
                if (entityInfo.SubType != EntityInfoSubType.MissileInfo) return;

                long key = entityInfo.EntityID;
                long relationID = entityInfo.MissileInfo.Value.LauncherID;
                EntitySource source = EntitySource.Remote;
                EntityRelation relation = EntityRelation.Me;
                EntityInfoExt entityInfoExt = new EntityInfoExt(entityInfo, source, relation, relationID);
                if (!MyMissilesExt.ContainsKey(key))
                {
                    MyMissilesExt.Add(key, entityInfoExt);
                }
                else
                {
                    var original = MyMissilesExt[key];
                    MyMissilesExt[key] = original.Merge(entityInfoExt);
                }
            }

            private void RemoveMissile(long entityID)
            {
                MyMissilesExt.Remove(entityID);
            }

            private void RegisterMissileAddress(long address, long missileID)
            {
                _addressMissileIDMap[address] = missileID;
                _missileIDAddressMap[missileID] = address;
            }

            private void RegisterMissileTarget(long address, long targetID)
            {
                _addressTargetIDMap[address] = targetID;
            }

            private void UnregisterMissile(long address)
            {
                long missileID;
                if (_addressMissileIDMap.TryGetValue(address, out missileID))
                {
                    _missileIDAddressMap.Remove(missileID);
                }
                _addressMissileIDMap.Remove(address);
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
                MissileBays.ForEach(bay => SelectBay(bay));
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
                if ((Time - _lastLaunch) < 1f || !_selectedBays.Contains(bay)) return;

                bay.Launch(targetID);
                _lastLaunch = Time;
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
                    while ((Time - _lastLaunch) < 1f)
                    {
                        yield return loopCounter++;
                    }
                }
                yield return loopCounter;
            }

            private void SyncClocks()
            {
                _lastClockSync = Time;
                double globalTime = SystemCoordinator.GlobalTime;

                foreach (long address in _addressMissileIDMap.Keys.ToList())
                {
                    string command = $"SYNC_CLOCK {globalTime}";
                    CommunicationHandler0.SendUnicast(command, address, "Commands", true);
                }
            }

            public void AbortMissile(long missileID, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return;
                }
                AbortMissile(missileID);
            }

            private void AbortMissile(long missileID)
            {
                if (!_missileIDAddressMap.ContainsKey(missileID)) return;
                long address = _missileIDAddressMap[missileID];
                if (CommunicationHandler0.CanReach(address))
                {
                    string command = "ABORT";
                    CommunicationHandler0.SendUnicast(command, address, "Commands", true);
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

            public override string ToString()
            {
                return $"SLCTD BAYS: {NumSelectedBays}/{NumBays}\nRDY BAYS: {NumReadyBays}/{NumBays}\nTRCKD MISLS: {NumMissiles}\nFIRE CTRL: {(FireControlAvail ? "AVAIL" : "IN USE")}";
            }
        }
    }
}
