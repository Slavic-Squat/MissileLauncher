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
        public class TargetCoordinator
        {
            #region Parts
            private CommunicationHandler _communicationHandler;
            #endregion

            #region Fields
            private long _selfID;
            private IMyCubeBlock _referenceBlock;
            #endregion

            #region Properties
            public int ID { get; private set; }

            private Dictionary<long, EntityInfoExt> _targetsLocal = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, TargetInfo> _targetsRemote = new Dictionary<long, TargetInfo>();
            private Dictionary<long, MissileInfoLite> _missilesRemote = new Dictionary<long, MissileInfoLite>();
            public HashSet<long> NeutralIDs { get; private set; }
            public HashSet<long> HostileIDs { get; private set; }
            public HashSet<long> FriendlyIDs { get; private set; }
            #endregion

            public enum TargetRelation : byte
            {
                Neutral, Friendly, Hostile
            }

            public TargetCoordinator(int id, long selfID, IMyCubeBlock referenceBlock, CommunicationHandler communicationHandler)
            {
                ID = id;
                _selfID = selfID;
                _referenceBlock = referenceBlock;
                _communicationHandler = communicationHandler;
                _communicationHandler.RegisterBroadcastListener("TargetInfo");

                NeutralIDs = new HashSet<long>();
                HostileIDs = new HashSet<long>();
                FriendlyIDs = new HashSet<long>();
            }

            public void Run(DateTime time)
            {
                MyIGCMessage message;
                if (_communicationHandler.TryRetrieveMessage("TargetInfo", out message))
                {
                    object messageObject = Deserializer.Deserialize(message.Data as string);
                    if (messageObject is TargetInfo)
                    {
                        AddRemoteTarget((TargetInfo)messageObject);
                    }
                    else if (messageObject is MissileInfoLite)
                    {
                        AddRemoteMissile((MissileInfoLite)messageObject);
                    }
                }

                foreach (var targetKey in _targetsLocal.Keys.ToList())
                {
                    TimeSpan timeSinceLastDetection = time - _targetsLocal[targetKey].TimeRecorded;

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoteLocalTarget(targetKey);
                    }
                }

                foreach (var targetKey in _targetsRemote.Keys.ToList())
                {
                    TimeSpan timeSinceLastDetection = time - _targetsRemote[targetKey].TimeRecorded;

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveRemoteTarget(targetKey);
                    }
                }

                foreach (var targetInfoExt in _targetsLocal.Values)
                {
                    byte[] data = targetInfoExt.Info.Serialize();
                    _communicationHandler.SendBroadcast(data, "TargetInfo");
                }
            }

            public void AddRemoteTarget(TargetInfo targetInfo)
            {
                var entityID = targetInfo.EntityID;
                var relationID = entityID;

                if (entityID == _selfID)
                {
                    return;
                }

                if (!NeutralIDs.Contains(relationID) && !HostileIDs.Contains(relationID) && !FriendlyIDs.Contains(relationID))
                {
                    SetTargetRelation(relationID, TargetRelation.Neutral);
                }

                if (!_targetsRemote.ContainsKey(entityID))
                {
                    _targetsRemote.Add(entityID, targetInfo);
                }
                else if (_targetsRemote[entityID].TimeRecorded < targetInfo.TimeRecorded)
                {
                    _targetsRemote[entityID] = targetInfo;
                }
            }

            public void AddRemoteMissile(MissileInfoLite missileInfo)
            {
                var entityID = missileInfo.EntityID;
                var relationID = missileInfo.LauncherID;

                if (relationID == _selfID)
                {
                    return;
                }

                if (!NeutralIDs.Contains(relationID) && !HostileIDs.Contains(relationID) && !FriendlyIDs.Contains(relationID))
                {
                    SetTargetRelation(relationID, TargetRelation.Neutral);
                }

                if (!_missilesRemote.ContainsKey(entityID))
                {
                    _missilesRemote.Add(entityID, missileInfo);
                }
                else if (_missilesRemote[entityID].TimeRecorded < missileInfo.TimeRecorded)
                {
                    _missilesRemote[entityID] = missileInfo;
                }
            }

            public void AddLocalTarget(EntityInfoExt targetInfoExt)
            {
                var entityID = targetInfoExt.EntityID;
                var relationID = entityID;
                if (entityID == _selfID)
                {
                    return;
                }

                if (!NeutralIDs.Contains(relationID) && !HostileIDs.Contains(relationID) && !FriendlyIDs.Contains(relationID))
                {
                    SetTargetRelation(relationID, TargetRelation.Neutral);
                }

                if (!_targetsLocal.ContainsKey(entityID))
                {
                    _targetsLocal.Add(entityID, targetInfoExt);
                }
                else
                {
                    _targetsLocal[entityID].Merge(targetInfoExt);
                }
            }

            public void RemoveRemoteTarget(long entityID)
            {
                _targetsRemote.Remove(entityID);
            }

            public void RemoteLocalTarget(long entityID)
            {
                _targetsLocal.Remove(entityID);
            }

            public void RemoteRemoteMissile(long entityID)
            {
                _missilesRemote.Remove(entityID);
            }

            public void SetTargetRelation(long entityID, TargetRelation relation)
            {
                switch (relation)
                {
                    case TargetRelation.Neutral:
                        HostileIDs.Remove(entityID);
                        FriendlyIDs.Remove(entityID);
                        NeutralIDs.Add(entityID);
                        break;

                    case TargetRelation.Friendly:
                        NeutralIDs.Remove(entityID);
                        HostileIDs.Remove(entityID);
                        FriendlyIDs.Add(entityID);
                        break;

                    case TargetRelation.Hostile:
                        NeutralIDs.Remove(entityID);
                        FriendlyIDs.Remove(entityID);
                        HostileIDs.Add(entityID);
                        break;
                }
            }

            public Dictionary<long, EntityInfoExt> GetAllTargets()
            {
                var allTargets = new Dictionary<long, EntityInfoExt>(_targetsLocal);

                foreach(var targetKVP in _targetsRemote)
                {
                    TargetInfo targetInfo = targetKVP.Value;
                    long key = targetInfo.EntityID;
                    long relationKey = targetInfo.EntityID;

                    EntitySource source = EntitySource.Remote;
                    EntityRelation relation;

                    if (NeutralIDs.Contains(key))
                    {
                        relation = EntityRelation.Neutral;
                    }
                    else if (FriendlyIDs.Contains(key))
                    {
                        relation = EntityRelation.Friendly;
                    }
                    else if (HostileIDs.Contains(key))
                    {
                        relation = EntityRelation.Hostile;
                    }
                    else if (relationKey == _selfID)
                    {
                        relation = EntityRelation.Me;
                    }
                    else
                    {
                        relation = EntityRelation.Neutral;
                    }
                    var entityInfoExt = new EntityInfoExt(targetInfo, source, relation);
                    
                    if (allTargets.ContainsKey(key))
                    {
                        allTargets[key].Merge(entityInfoExt);
                    }
                    else
                    {
                        allTargets.Add(key, entityInfoExt);
                    }
                }

                foreach(var missileKVP in _missilesRemote)
                {
                    MissileInfoLite missileInfo = missileKVP.Value;
                    long key = missileInfo.EntityID;
                    long relationKey = missileInfo.LauncherID;

                    EntitySource source = EntitySource.Remote;
                    EntityRelation relation;

                    if (NeutralIDs.Contains(key))
                    {
                        relation = EntityRelation.Neutral;
                    }
                    else if (FriendlyIDs.Contains(key))
                    {
                        relation = EntityRelation.Friendly;
                    }
                    else if (HostileIDs.Contains(key))
                    {
                        relation = EntityRelation.Hostile;
                    }
                    else if (relationKey == _selfID)
                    {
                        relation = EntityRelation.Me;
                    }
                    else
                    {
                        relation = EntityRelation.Neutral;
                    }

                    var entityInfoExt = new EntityInfoExt(missileInfo, source, relation);

                    if (allTargets.ContainsKey(key))
                    {
                        allTargets[key].Merge(entityInfoExt);
                    }
                    else
                    {
                        allTargets.Add(key, entityInfoExt);
                    }
                }

                return allTargets;
            }
        }
    }
}
