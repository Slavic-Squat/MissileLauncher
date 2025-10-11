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

            public TargetCoordinator(int id, CommunicationHandler communicationHandler)
            {
                ID = id;
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

            private void AddRemoteTarget(EntityInfo entityInfo)
            {
                var entityID = entityInfo.EntityID;
                var relationID = entityID;

                if (entityID == SystemCoordinator.SelfID)
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
                    var original = _targetsRemote[entityID];
                    _targetsRemote[entityID] = original.Merge(entityInfo);
                }
            }

            private void AddRemoteFriendly(EntityInfo entityInfo)
            {
                var entityID = entityInfo.EntityID;
                var relationID = entityID;

                if (entityID == SystemCoordinator.SelfID)
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
                    var original = _friendlysRemote[entityID];
                    _friendlysRemote[entityID] = original.Merge(entityInfo);
                }
            }

            private void AddRemoteMissile(EntityInfo entityInfo)
            {
                if (entityInfo.SubType != EntityInfoSubType.MissileInfoLite)
                {
                    return;
                }
                var missileInfo = entityInfo.MissileInfoLite.Value;
                var entityID = entityInfo.EntityID;

                var relationID = missileInfo.LauncherID;

                if (relationID == SystemCoordinator.SelfID)
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
                    var original = _missilesRemote[entityID];
                    _missilesRemote[entityID] = original.Merge(entityInfo);
                }
            }

            public void AddLocalTarget(EntityInfoExt targetInfoExt)
            {
                var entityID = targetInfoExt.EntityID;
                var relationID = entityID;
                if (entityID == SystemCoordinator.SelfID)
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
                    var original = _targetsLocal[entityID];
                    _targetsLocal[entityID] = original.Merge(targetInfoExt);
                }
            }

            private void RemoveRemoteTarget(long entityID)
            {
                _targetsRemote.Remove(entityID);
            }

            private void RemoteLocalTarget(long entityID)
            {
                _targetsLocal.Remove(entityID);
            }

            private void RemoteRemoteMissile(long entityID)
            {
                _missilesRemote.Remove(entityID);
            }

            private void RemoteRomoteFriendly(long entityID)
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
                    else if (relationKey == SystemCoordinator.SelfID)
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
                        var original = allTargets[key];
                        allTargets[key] = original.Merge(entityInfoExt);
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
                    else if (relationKey == SystemCoordinator.SelfID)
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
                        var original = allTargets[key];
                        allTargets[key] = original.Merge(entityInfoExt);
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
                        var original = allTargets[key];
                        allTargets[key] = original.Merge(entityInfoExt);
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
