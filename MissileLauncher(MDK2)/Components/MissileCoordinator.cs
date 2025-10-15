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
            #region Properties
            public int ID { get; private set; }
            public DateTime Time { get; private set; }
            public Dictionary<long, EntityInfoExt> MyMissilesExt { get; private set; }
            public ControlStation Station { get; private set; }
            public bool FireControlAvail => Station == null;
            public int NumBays { get; private set; }
            public int NumSelectedBays => _selectedBays.Count;
            public int NumReadyBays { get; private set; }
            public bool IsLaunching => _launchCoroutine != null;
            public int NumMissiles => MyMissilesExt.Count;
            #endregion

            #region Components
            public List<MissileBay> MissileBays { get; private set; }
            #endregion

            private CommunicationHandler _communicationHandler;

            private HashSet<MissileBay> _selectedBays = new HashSet<MissileBay>();
            private Dictionary<long, long> _addressMissileIDMap = new Dictionary<long, long>();
            private Dictionary<long, long> _addressTargetIDMap = new Dictionary<long, long>();
            private Dictionary<long, long> _missileIDAddressMap = new Dictionary<long, long>();

            private Dictionary<long, EntityInfoExt> _targetInfo = new Dictionary<long, EntityInfoExt>();

            private IEnumerator<bool> _launchCoroutine;

            private DateTime _lastClockSync = DateTime.MinValue;
            private DateTime _lastLaunch = DateTime.MinValue;

            public MissileCoordinator(int id, int numBays, CommunicationHandler communicationHandler, Dictionary<long, EntityInfoExt> targetInfo)
            {
                ID = id;
                NumBays = numBays;
                _communicationHandler = communicationHandler;
                _targetInfo = targetInfo;
                Init();
            }

            private void Init()
            {
                MissileBays = new List<MissileBay>();
                MyMissilesExt = new Dictionary<long, EntityInfoExt>();
                for (int i = 0; i < NumBays; i++)
                {
                    MissileBays.Add(new MissileBay(i));
                }

                _communicationHandler.RegisterTag("MyMissiles", true);
            }

            public void Run(DateTime time)
            {
                Time = time;

                NumReadyBays = 0;
                foreach (var bay in MissileBays)
                {
                    bay.Run(time);
                    if (bay.Status == BayStatus.Ready) NumReadyBays++;
                }

                while (_communicationHandler.HasMessage("MyMissiles", true))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("MyMissiles", true, out message))
                    {
                        if (!_addressMissileIDMap.ContainsKey(message.Source))
                        {
                            continue;
                        }
                        object messageObject = Deserializer.Deserialize(message.Data as string);
                        if (messageObject is EntityInfo)
                        {
                            AddMissile((EntityInfo)messageObject);
                        }
                    }
                }

                foreach (var missileAddress in _addressTargetIDMap.Keys.ToList())
                {
                    if (!_communicationHandler.CanReach(missileAddress))
                    {
                        long missileID = _addressMissileIDMap[missileAddress];
                        RemoveMissile(missileID);
                        UnregisterMissileAddress(missileAddress);
                        continue;
                    }

                    byte[] selfData = SystemCoordinator.SelfInfo.Serialize();
                    _communicationHandler.SendUnicast(selfData, missileAddress, "MyMissileLauncherInfo", true);

                    long targetID = _addressTargetIDMap[missileAddress];
                    if (_targetInfo.ContainsKey(targetID))
                    {
                        byte[] targetData = _targetInfo[targetID].Info.Serialize();
                        _communicationHandler.SendUnicast(targetData, missileAddress, "MyMissileTargetInfo", true);
                    }
                }

                if (time - _lastClockSync > TimeSpan.FromSeconds(10))
                {
                    SyncClocks();
                }

                if (_launchCoroutine != null && !_launchCoroutine.MoveNext())
                {
                    _launchCoroutine = null;
                }
            }

            private bool AddMissile(EntityInfo entityInfo)
            {
                if (entityInfo.SubType != EntityInfoSubType.MissileInfo) return false;

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
                return true;
            }

            private bool RemoveMissile(long entityID)
            {
                MyMissilesExt.Remove(entityID);
                return true;
            }

            private bool RegisterMissileAddress(long address, long missileID, long targetID)
            {
                _addressMissileIDMap[address] = missileID;
                _addressTargetIDMap[address] = targetID;
                _missileIDAddressMap[missileID] = address;
                return true;
            }

            private bool UnregisterMissileAddress(long address)
            {
                long missileID;
                if (_addressMissileIDMap.TryGetValue(address, out missileID))
                {
                    _missileIDAddressMap.Remove(missileID);
                }
                _addressMissileIDMap.Remove(address);
                _addressTargetIDMap.Remove(address);
                return true;
            }

            public bool SelectBay(MissileBay bay, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller) || bay == null)
                {
                    return false;
                }
                return SelectBay(bay);
            }

            private bool SelectBay(MissileBay bay)
            {
                if (!bay.IsSelectable) return false;
                _selectedBays.Add(bay);
                bay.IsSelected = true;
                return true;
            }

            public bool DeselectBay(MissileBay bay, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller) || bay == null)
                {
                    return false;
                }
                return DeselectBay(bay);
            }

            private bool DeselectBay(MissileBay bay)
            {
                bay.IsSelected = false;
                _selectedBays.Remove(bay);
                return true;
            }

            public bool ToggleBaySelection(MissileBay bay, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller) || bay == null)
                {
                    return false;
                }
                return ToggleBaySelection(bay);
            }

            private bool ToggleBaySelection(MissileBay bay)
            {
                if (_selectedBays.Contains(bay))
                {
                    return DeselectBay(bay);
                }
                else
                {
                    return SelectBay(bay);
                }
            }

            public bool ClearSelectedBays(object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return false;
                }
                return ClearSelectedBays();
            }

            private bool ClearSelectedBays()
            {
                foreach (var bay in _selectedBays.ToList())
                {
                    DeselectBay(bay);
                }
                return true;
            }

            public bool SelectAllBays(object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return false;
                }
                return SelectAllBays();
            }

            private bool SelectAllBays()
            {
                MissileBays.ForEach(bay => SelectBay(bay));
                return true;
            }

            public bool LaunchMissile(long targetID, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller) || IsLaunching)
                {
                    return false;
                }
                if (_selectedBays.Count == 0) return false;
                var bay = _selectedBays.First();
                return LaunchMissile(bay, targetID);
            }

            private bool LaunchMissile(MissileBay bay, long targetID)
            {
                if (Time - _lastLaunch < TimeSpan.FromSeconds(1) || !_selectedBays.Contains(bay)) return false;

                long missileID = bay.MissileID;
                long missileAddress = bay.MissileAddress;
                if (!bay.Launch())
                {
                    return false;
                }
                RegisterMissileAddress(missileAddress, missileID, targetID);
                _lastLaunch = Time;
                DeselectBay(bay);
                return true;
            }

            public bool LaunchMissiles(long targetID, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return false;
                }
                return LaunchMissiles(targetID);
            }

            private bool LaunchMissiles(long targetID)
            {
                if (IsLaunching) return false;
                _launchCoroutine = HandleLaunch(targetID);
                return true;
            }

            private IEnumerator<bool> HandleLaunch(long targetID)
            {
                foreach (var bayID in _selectedBays.ToList())
                {
                    while (!LaunchMissile(bayID, targetID))
                    {
                        yield return false;
                    }
                }
                yield return true;
            }

            private bool SyncClocks()
            {
                _lastClockSync = Time;

                foreach (long address in _addressMissileIDMap.Keys.ToList())
                {
                    string command = $"SYNC_CLOCK {Time.Ticks}";
                    List<byte> commandData = new List<byte>()
                    {
                        (byte)SerializedTypes.Command,
                    };
                    commandData.AddRange(Encoding.ASCII.GetBytes(command));
                    byte[] commandBytes = commandData.ToArray();
                    _communicationHandler.SendUnicast(commandBytes, address, "MyMissileCommands", true);
                }
                return true;
            }

            public bool AbortMissile(long missileID, object caller)
            {
                if (caller == null || FireControlAvail || !ReferenceEquals(Station, caller))
                {
                    return false;
                }
                return AbortMissile(missileID);
            }

            private bool AbortMissile(long missileID)
            {
                if (!_missileIDAddressMap.ContainsKey(missileID)) return false;
                long address = _missileIDAddressMap[missileID];
                if (_communicationHandler.CanReach(address))
                {
                    string command = "ABORT";
                    List<byte> commandData = new List<byte>()
                    {
                        (byte)SerializedTypes.Command,
                    };
                    commandData.AddRange(Encoding.ASCII.GetBytes(command));
                    byte[] commandBytes = commandData.ToArray();
                    _communicationHandler.SendUnicast(commandBytes, address, "MyMissileCommands", true);
                    return true;
                }
                return false;
            }

            public bool GiveFireControl(ControlStation station)
            {
                if (station == null || !FireControlAvail || ReferenceEquals(Station, station))
                {
                    return false;
                }
                Station = station;
                return true;
            }

            public bool RevokeFireControl(ControlStation station)
            {
                if (station == null || FireControlAvail || !ReferenceEquals(Station, station))
                {
                    return false;
                }
                Station = null;
                ClearSelectedBays();
                return true;
            }

            public override string ToString()
            {
                return $"SLCTD BAYS: {NumSelectedBays}/{NumBays}\nRDY BAYS: {NumReadyBays}/{NumBays}\nTRCKD MISLS: {NumMissiles}\nFIRE CTRL: {(FireControlAvail ? "AVAIL" : "IN USE")}";
            }
        }
    }
}
