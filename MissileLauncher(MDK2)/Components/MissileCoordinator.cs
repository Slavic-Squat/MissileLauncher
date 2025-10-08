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
            public Dictionary<long, EntityInfoExt> ActiveMissilesExt
            {
                get
                {
                    if (_activeMissilesStale)
                    {
                        _activeMissilesExt = GetAllMyMissiles();
                        _activeMissilesStale = false;
                    }
                    return _activeMissilesExt;
                }
            }
            #endregion

            #region Components
            public List<MissileBay> MissileBays { get; private set; }
            public HashSet<int> SelectedBays { get; private set; }
            #endregion

            private CommunicationHandler _communicationHandler;
            private IMyCubeBlock _referenceBlock;
            private long _selfID;

            private Dictionary<long, MissileInfo> _activeMissiles = new Dictionary<long, MissileInfo>();
            private Dictionary<long, EntityInfoExt> _activeMissilesExt = new Dictionary<long, EntityInfoExt>();
            private bool _activeMissilesStale = true;

            private Dictionary<long, EntityInfoExt> _targetInfo = new Dictionary<long, EntityInfoExt>();

            private IEnumerator<bool> _launchCoroutine;

            public MissileCoordinator(int id, int numberOfMissileBays, IMyCubeBlock referenceBlock, long selfID, CommunicationHandler communicationHandler, Dictionary<long, EntityInfoExt> targetInfo)
            {
                ID = id;
                _referenceBlock = referenceBlock;
                _selfID = selfID;
                _communicationHandler = communicationHandler;
                _communicationHandler.RegisterTag("MyMissiles");
                _targetInfo = targetInfo;

                MissileBays = new List<MissileBay>();
                SelectedBays = new HashSet<int>();
                for (int i = 0; i < numberOfMissileBays; i++)
                {
                    MissileBays.Add(new MissileBay(i, _selfID, _communicationHandler.SelfAddress, _activeMissiles));
                }
            }

            public void Run(DateTime time)
            {
                Time = time;
                _activeMissilesStale = true;

                foreach (var bay in MissileBays)
                {
                    bay.Run(time);
                }

                while (_communicationHandler.HasMessage("MyMissiles"))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("MyMissiles", out message))
                    {
                        object messageObject = Deserializer.Deserialize(message.Data as string);
                        if (messageObject is MissileInfo)
                        {
                            AddMissile((MissileInfo)messageObject);
                        }
                    }
                }

                foreach (var missile in _activeMissiles.Values.ToList())
                {
                    if (!_communicationHandler.CanReach(missile.Address))
                    {
                        RemoveMissile(missile.EntityID);
                    }

                    if (_targetInfo.ContainsKey(missile.TargetID))
                    {
                        byte[] targetData = _targetInfo[missile.TargetID].Info.Serialize();
                        _communicationHandler.SendUnicast(targetData, missile.Address, "TargetInfo");
                    }
                }

                if (_launchCoroutine != null && !_launchCoroutine.MoveNext())
                {
                    _launchCoroutine = null;
                }
            }

            private void AddMissile(MissileInfo missileInfo)
            {
                long key = missileInfo.EntityID;
                if (!_activeMissiles.ContainsKey(key))
                {
                    _activeMissiles.Add(key, missileInfo);
                }
                else if (_activeMissiles[key].TimeRecorded < missileInfo.TimeRecorded)
                {
                    _activeMissiles[key] = missileInfo;
                }
            }

            private void RemoveMissile(long entityID)
            {
                _activeMissiles.Remove(entityID);
            }

            private Dictionary<long, EntityInfoExt> GetAllMyMissiles()
            {
                Dictionary<long, EntityInfoExt> allMyMissiles = new Dictionary<long, EntityInfoExt>();

                foreach (var missileInfo in _activeMissiles.Values)
                {
                    long key = missileInfo.EntityID;
                    EntitySource source = EntitySource.Remote;
                    EntityRelation relation = EntityRelation.Me;
                    float distance = Vector3.Distance(missileInfo.Position, _referenceBlock.GetPosition());

                    allMyMissiles[key] = new EntityInfoExt(missileInfo, source, relation);
                }

                return allMyMissiles;
            }

            public void SelectBay(int bayID)
            {
                if (bayID >= 0 && bayID < MissileBays.Count)
                {
                    var bay = MissileBays[bayID];
                    if (!bay.IsSelectable) return;
                    SelectedBays.Add(bayID);
                    bay.IsSelected = true;
                }
            }

            public void DeselectBay(int bayID)
            {
                if (bayID >= 0 && bayID < MissileBays.Count)
                {
                    var bay = MissileBays[bayID];
                    bay.IsSelected = false;
                    SelectedBays.Remove(bayID);
                }
            }

            public void ToggleBaySelection(int bayID)
            {
                if (SelectedBays.Contains(bayID))
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
                foreach (int bayID in SelectedBays.ToList())
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
                foreach (var bayID in SelectedBays.ToList())
                {
                    while (Time - timeOfLastLaunch < TimeSpan.FromSeconds(1))
                    {
                        yield return false;
                    }
                    while (MissileBays[bayID].Status == BayStatus.Loaded && !MissileBays[bayID].InitMissile())
                    {
                        DebugEcho("Failed to initialize missile.");
                        yield return false;
                    }

                    if (MissileBays[bayID].Launch(targetID))
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
