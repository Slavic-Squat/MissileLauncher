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
            private Dictionary<long, EntityInfo> _targetsRemote = new Dictionary<long, EntityInfo>();
            private Dictionary<long, EntityInfo> _missilesRemote = new Dictionary<long, EntityInfo>();
            private Dictionary<long, EntityInfo> _friendlysRemote = new Dictionary<long, EntityInfo>();

            private Dictionary<long, EntityInfoExt> _allTargetsExt = new Dictionary<long, EntityInfoExt>();
            private bool _allTargetsStale = true;

            public Dictionary<long, EntityInfoExt> AllTargetsExt
            {
                get
                {
                    if (_allTargetsStale)
                    {
                        _allTargetsExt = GetAllTargets();
                        _allTargetsStale = false;
                    }
                    return _allTargetsExt;
                }
            }
            public HashSet<long> NeutralIDs { get; private set; }
            public HashSet<long> HostileIDs { get; private set; }
            public HashSet<long> FriendlyIDs { get; private set; }
            #endregion

            public TargetCoordinator(int id, long selfID, IMyCubeBlock referenceBlock, CommunicationHandler communicationHandler)
            {
                ID = id;
                _selfID = selfID;
                _referenceBlock = referenceBlock;
                _communicationHandler = communicationHandler;
                _communicationHandler.RegisterBroadcastListener("TargetInfo");
                _communicationHandler.RegisterBroadcastListener("MissileInfo");
                _communicationHandler.RegisterBroadcastListener("FriendlyInfo");

                NeutralIDs = new HashSet<long>();
                HostileIDs = new HashSet<long>();
                FriendlyIDs = new HashSet<long>();
            }

            public void Run(DateTime time)
            {
                _allTargetsStale = true;

                
                while (_communicationHandler.HasMessage("TargetInfo"))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("TargetInfo", out message))
                    {
                        object messageObject = Deserializer.Deserialize(message.Data as string);
                        if (messageObject is EntityInfo)
                        {
                            AddRemoteTarget((EntityInfo)messageObject);
                        }
                    }
                }

                while (_communicationHandler.HasMessage("FriendlyInfo"))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("FriendlyInfo", out message))
                    {
                        object messageObject = Deserializer.Deserialize(message.Data as string);
                        if (messageObject is EntityInfo)
                        {
                            AddRemoteFriendly((EntityInfo)messageObject);
                        }
                    }
                }

                while (_communicationHandler.HasMessage("MissileInfo"))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("MissileInfo", out message))
                    {
                        object messageObject = Deserializer.Deserialize(message.Data as string);
                        if (messageObject is EntityInfo)
                        {
                            AddRemoteMissile((EntityInfo)messageObject);
                        }
                    }
                }

                foreach (var friendlyKey in _friendlysRemote.Keys.ToList())
                {
                    TimeSpan timeSinceLastDetection = time - _friendlysRemote[friendlyKey].TimeRecorded;
                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoteRomoteFriendly(friendlyKey);
                    }
                }

                foreach (var missileKey in _missilesRemote.Keys.ToList())
                {
                    TimeSpan timeSinceLastDetection = time - _missilesRemote[missileKey].TimeRecorded;
                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoteRemoteMissile(missileKey);
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

            public void AddRemoteTarget(EntityInfo entityInfo)
            {
                var entityID = entityInfo.EntityID;
                var relationID = entityID;

                if (entityID == _selfID)
                {
                    return;
                }

                if (!NeutralIDs.Contains(relationID) && !HostileIDs.Contains(relationID) && !FriendlyIDs.Contains(relationID))
                {
                    SetTargetRelation(relationID, EntityRelation.Neutral);
                }

                if (!_targetsRemote.ContainsKey(entityID))
                {
                    _targetsRemote.Add(entityID, entityInfo);
                }
                else
                {
                    _targetsRemote[entityID].Merge(entityInfo);
                }
            }

            public void AddRemoteFriendly(EntityInfo entityInfo)
            {
                var entityID = entityInfo.EntityID;
                var relationID = entityID;

                if (entityID == _selfID)
                {
                    return;
                }

                SetTargetRelation(relationID, EntityRelation.Friendly);

                if (!_friendlysRemote.ContainsKey(entityID))
                {
                    _friendlysRemote.Add(entityID, entityInfo);
                }
                else
                {
                    _friendlysRemote[entityID].Merge(entityInfo);
                }
            }

            public void AddRemoteMissile(EntityInfo entityInfo)
            {
                if (entityInfo.SubType != EntityInfoSubType.MissileInfoLite)
                {
                    return;
                }
                var missileInfo = entityInfo.MissileInfoLite.Value;
                var entityID = entityInfo.EntityID;

                var relationID = missileInfo.LauncherID;

                if (relationID == _selfID)
                {
                    return;
                }

                if (!NeutralIDs.Contains(relationID) && !HostileIDs.Contains(relationID) && !FriendlyIDs.Contains(relationID))
                {
                    SetTargetRelation(relationID, EntityRelation.Neutral);
                }

                if (!_missilesRemote.ContainsKey(entityID))
                {
                    _missilesRemote.Add(entityID, entityInfo);
                }
                else
                {
                    _missilesRemote[entityID].Merge(entityInfo);
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

                SetTargetRelation(relationID, targetInfoExt.Relation);

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

            public void RemoteRomoteFriendly(long entityID)
            {
                _friendlysRemote.Remove(entityID);
            }

            public void SetTargetRelation(long entityID, EntityRelation relation)
            {
                switch (relation)
                {
                    case EntityRelation.Neutral:
                        HostileIDs.Remove(entityID);
                        FriendlyIDs.Remove(entityID);
                        NeutralIDs.Add(entityID);
                        break;

                    case EntityRelation.Friendly:
                        NeutralIDs.Remove(entityID);
                        HostileIDs.Remove(entityID);
                        FriendlyIDs.Add(entityID);
                        break;

                    case EntityRelation.Hostile:
                        NeutralIDs.Remove(entityID);
                        FriendlyIDs.Remove(entityID);
                        HostileIDs.Add(entityID);
                        break;
                    case EntityRelation.Me:
                        NeutralIDs.Remove(entityID);
                        FriendlyIDs.Remove(entityID);
                        HostileIDs.Remove(entityID);
                        break;
                }
            }

            private Dictionary<long, EntityInfoExt> GetAllTargets()
            {
                var allTargets = new Dictionary<long, EntityInfoExt>(_targetsLocal);

                foreach(var target in _targetsRemote.Values)
                {
                    long key = target.EntityID;
                    long relationKey = target.EntityID;

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
                    var entityInfoExt = new EntityInfoExt(target, source, relation);
                    
                    if (allTargets.ContainsKey(key))
                    {
                        allTargets[key].Merge(entityInfoExt);
                    }
                    else
                    {
                        allTargets.Add(key, entityInfoExt);
                    }
                }

                foreach(var missile in _missilesRemote.Values)
                {
                    long key = missile.EntityID;
                    long relationKey = missile.MissileInfoLite.Value.LauncherID;

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

                    var entityInfoExt = new EntityInfoExt(missile, source, relation);

                    if (allTargets.ContainsKey(key))
                    {
                        allTargets[key].Merge(entityInfoExt);
                    }
                    else
                    {
                        allTargets.Add(key, entityInfoExt);
                    }
                }

                foreach (var friendly in _friendlysRemote.Values)
                {
                    long key = friendly.EntityID;
                    long relationKey = friendly.EntityID;

                    EntitySource source = EntitySource.Remote;
                    EntityRelation relation = EntityRelation.Friendly;

                    var entityInfoExt = new EntityInfoExt(friendly, source, relation);

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
