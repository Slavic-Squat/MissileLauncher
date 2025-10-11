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
            #endregion

            #region Components
            public List<MissileBay> MissileBays { get; private set; }
            #endregion

            private CommunicationHandler _communicationHandler;

            private HashSet<int> _selectedBays = new HashSet<int>();
            private Dictionary<long, long> _addressMissileIDMap = new Dictionary<long, long>();
            private Dictionary<long, long> _addressTargetIDMap = new Dictionary<long, long>();

            private Dictionary<long, EntityInfoExt> _targetInfo = new Dictionary<long, EntityInfoExt>();

            private IEnumerator<bool> _launchCoroutine;

            private DateTime _lastClockSync = DateTime.MinValue;

            public MissileCoordinator(int id, int numberOfMissileBays, CommunicationHandler communicationHandler, Dictionary<long, EntityInfoExt> targetInfo)
            {
                ID = id;
                _communicationHandler = communicationHandler;
                _communicationHandler.RegisterTag("MyMissiles");
                _targetInfo = targetInfo;

                MissileBays = new List<MissileBay>();
                for (int i = 0; i < numberOfMissileBays; i++)
                {
                    MissileBays.Add(new MissileBay(i));
                }
            }

            public void Run(DateTime time)
            {
                Time = time;

                foreach (var bay in MissileBays)
                {
                    bay.Run(time);
                }

                Vector3 selfPos = SystemCoordinator.ReferencePosition;
                Vector3 selfVel = SystemCoordinator.ReferenceVelocity;
                long selfID = SystemCoordinator.SelfID;
                DateTime timeRecorded = time;

                EntityInfo self = new EntityInfo(selfID, selfPos, selfVel, timeRecorded);

                while (_communicationHandler.HasMessage("MyMissiles"))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("MyMissiles", out message))
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

                    byte[] selfData = self.Serialize();
                    _communicationHandler.SendUnicast(selfData, missileAddress, "LauncherInfo");

                    if (time - _lastClockSync > TimeSpan.FromSeconds(10))
                    {
                        string command = $"SYNC_CLOCK {Time}";
                        List<byte> commandData = new List<byte>()
                        {
                            (byte)SerializedTypes.Command
                        };
                        commandData.AddRange(Encoding.ASCII.GetBytes(command));
                        byte[] commandBytes = commandData.ToArray();
                        _communicationHandler.SendUnicast(commandBytes, missileAddress, "Commands");
                    }

                    long targetID = _addressTargetIDMap[missileAddress];
                    if (_targetInfo.ContainsKey(targetID))
                    {
                        byte[] targetData = _targetInfo[targetID].Info.Serialize();
                        _communicationHandler.SendUnicast(targetData, missileAddress, "TargetInfo");
                    }
                }

                if (_launchCoroutine != null && !_launchCoroutine.MoveNext())
                {
                    _launchCoroutine = null;
                }
            }

            private void AddMissile(EntityInfo entityInfo)
            {
                if (entityInfo.SubType != EntityInfoSubType.MissileInfo) return;

                long key = entityInfo.EntityID;
                EntitySource source = EntitySource.Remote;
                EntityRelation relation = EntityRelation.Me;
                EntityInfoExt entityInfoExt = new EntityInfoExt(entityInfo, source, relation);
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

            public void RegisterMissileAddress(long address, long missileID, long targetID)
            {
                _addressMissileIDMap[address] = missileID;
                _addressTargetIDMap[address] = targetID;
            }

            public void UnregisterMissileAddress(long address)
            {
                _addressMissileIDMap.Remove(address);
                _addressTargetIDMap.Remove(address);
            }

            public void SelectBay(int bayID)
            {
                if (bayID >= 0 && bayID < MissileBays.Count)
                {
                    var bay = MissileBays[bayID];
                    if (!bay.IsSelectable) return;
                    _selectedBays.Add(bayID);
                    bay.IsSelected = true;
                }
            }

            public void DeselectBay(int bayID)
            {
                if (bayID >= 0 && bayID < MissileBays.Count)
                {
                    var bay = MissileBays[bayID];
                    bay.IsSelected = false;
                    _selectedBays.Remove(bayID);
                }
            }

            public void ToggleBaySelection(int bayID)
            {
                if (_selectedBays.Contains(bayID))
                {
                    DeselectBay(bayID);
                }
                else
                {
                    SelectBay(bayID);
                }
            }

            public void ClearSelectedBays()
            {
                foreach (int bayID in _selectedBays.ToList())
                {
                    DeselectBay(bayID);
                }
            }

            public void LaunchMissiles(long targetID)
            {
                if (_launchCoroutine != null) return;
                _launchCoroutine = HandleLaunch(targetID);
            }

            private IEnumerator<bool> HandleLaunch(long targetID)
            {
                DateTime timeOfLastLaunch = DateTime.MinValue;
                foreach (var bayID in _selectedBays.ToList())
                {
                    while (Time - timeOfLastLaunch < TimeSpan.FromSeconds(1))
                    {
                        yield return false;
                    }
                    while (MissileBays[bayID].Status == BayStatus.Loaded && !MissileBays[bayID].TryInitMissile(Time))
                    {
                        DebugEcho("Failed to initialize missile.");
                        yield return false;
                    }

                    long missileID = MissileBays[bayID].MissileID;
                    long missileAddress = MissileBays[bayID].MissileAddress;

                    RegisterMissileAddress(missileAddress, missileID, targetID);

                    if (MissileBays[bayID].Launch())
                    {
                        timeOfLastLaunch = Time;
                    }                    
                    DeselectBay(bayID);
                }
                yield return true;
            }
        }
    }
}
