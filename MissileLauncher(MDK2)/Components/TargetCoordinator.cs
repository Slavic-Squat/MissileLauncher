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
            public double Time { get; private set; }

            private Dictionary<long, EntityInfoExt> _targetsLocal = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, EntityInfoExt> _targetsRemote = new Dictionary<long, EntityInfoExt>();

            public Dictionary<long, EntityInfoExt> AllTargetsExt { get; private set; }
            public HashSet<long> NeutralIDs { get; private set; }
            public HashSet<long> HostileIDs { get; private set; }
            public HashSet<long> FriendlyIDs { get; private set; }
            #endregion

            public TargetCoordinator(int id, CommunicationHandler communicationHandler)
            {
                ID = id;
                _communicationHandler = communicationHandler;
                Init();
            }

            private void Init()
            {
                _communicationHandler.RegisterBroadcastListener("FriendlyTargetInfo", true);
                _communicationHandler.RegisterBroadcastListener("AllMissiles", false);
                _communicationHandler.RegisterBroadcastListener("FriendlyInfo", true);

                AllTargetsExt = new Dictionary<long, EntityInfoExt>();
                NeutralIDs = new HashSet<long>();
                HostileIDs = new HashSet<long>();
                FriendlyIDs = new HashSet<long>();
            }

            public void Run(double time)
            {
                Time = time;
                while (_communicationHandler.HasMessage("FriendlyTargetInfo", true))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("FriendlyTargetInfo", true, out message))
                    {
                        object messageObject = Deserializer.Deserialize(message.Data as string);
                        if (messageObject is EntityInfo)
                        {
                            AddRemoteTarget((EntityInfo)messageObject, false);
                        }
                    }
                }

                while (_communicationHandler.HasMessage("FriendlyInfo", true))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("FriendlyInfo", true, out message))
                    {
                        object messageObject = Deserializer.Deserialize(message.Data as string);
                        if (messageObject is EntityInfo)
                        {
                            AddRemoteTarget((EntityInfo)messageObject, true);
                        }
                    }
                }

                while (_communicationHandler.HasMessage("AllMissiles", false))
                {
                    MyIGCMessage message;
                    if (_communicationHandler.TryRetrieveMessage("AllMissiles", false, out message))
                    {
                        object messageObject = Deserializer.Deserialize(message.Data as string);
                        if (messageObject is EntityInfo)
                        {
                            AddRemoteTarget((EntityInfo)messageObject, false);
                        }
                    }
                }

                foreach (var targetKey in _targetsLocal.Keys.ToList())
                {
                    float timeSinceLastDetection = (float)(time - _targetsLocal[targetKey].TimeRecorded);

                    if (timeSinceLastDetection > 5f)
                    {
                        RemoteLocalTarget(targetKey);
                        continue;
                    }

                    var targetInfo = _targetsLocal[targetKey].Info;
                    byte[] data = targetInfo.Serialize();
                    _communicationHandler.SendBroadcast(data, "FriendlyTargetInfo", true);
                }

                foreach (var targetKey in _targetsRemote.Keys.ToList())
                {
                    float timeSinceLastDetection = (float)(time - _targetsRemote[targetKey].TimeRecorded);

                    if (timeSinceLastDetection > 5f)
                    {
                        RemoveRemoteTarget(targetKey);
                    }
                }
            }

            private bool AddRemoteTarget(EntityInfo entityInfo, bool friendly)
            {
                var entityID = entityInfo.EntityID;
                var relationID = entityID;

                if (entityInfo.Type == EntityType.Missile)
                {
                    if (entityInfo.SubType != EntityInfoSubType.MissileInfoLite)
                    {
                        return false;
                    }

                    relationID = entityInfo.MissileInfoLite.Value.LauncherID;
                }

                if (entityID == SystemCoordinator.SelfID || relationID == SystemCoordinator.SelfID)
                {
                    return false;
                }

                if (friendly)
                {
                    SetTargetRelation(relationID, EntityRelation.Friendly);
                }
                else if (!NeutralIDs.Contains(relationID) && !HostileIDs.Contains(relationID) && !FriendlyIDs.Contains(relationID))
                {
                    SetTargetRelation(relationID, EntityRelation.Neutral);
                }

                EntitySource source = EntitySource.Remote;
                EntityRelation relation = FriendlyIDs.Contains(relationID) ? EntityRelation.Friendly : HostileIDs.Contains(relationID) ? EntityRelation.Hostile : EntityRelation.Neutral;

                var entityInfoExt = new EntityInfoExt(entityInfo, source, relation, relationID);

                if (!_targetsRemote.ContainsKey(entityID))
                {
                    _targetsRemote.Add(entityID, entityInfoExt);
                }
                else
                {
                    var original = _targetsRemote[entityID];
                    _targetsRemote[entityID] = original.Merge(entityInfoExt);
                }

                if (!AllTargetsExt.ContainsKey(entityID))
                {
                    AllTargetsExt.Add(entityID, entityInfoExt);
                }
                else
                {
                    var original = AllTargetsExt[entityID];
                    AllTargetsExt[entityID] = original.Merge(entityInfoExt);
                }
                return true;
            }

            public bool AddLocalTarget(EntityInfoExt targetInfoExt)
            {
                var entityID = targetInfoExt.EntityID;
                var relationID = entityID;

                if (entityID == SystemCoordinator.SelfID)
                {
                    return false;
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

                if (!AllTargetsExt.ContainsKey(entityID))
                {
                    AllTargetsExt.Add(entityID, targetInfoExt);
                }
                else
                {
                    var original = AllTargetsExt[entityID];
                    AllTargetsExt[entityID] = original.Merge(targetInfoExt);
                }
                return true;
            }

            private bool RemoveRemoteTarget(long entityID)
            {
                _targetsRemote.Remove(entityID);

                if (AllTargetsExt.ContainsKey(entityID))
                {
                    var original = AllTargetsExt[entityID];
                    if (original.Source == EntitySource.Remote)
                    {
                        AllTargetsExt.Remove(entityID);
                    }
                    else if ((original.Source & EntitySource.Remote) != 0)
                    {
                        EntitySource newSource = original.Source & ~EntitySource.Remote;
                        var newInfo = new EntityInfoExt(original.Info, newSource, original.Relation, original.RelationID);
                        AllTargetsExt[entityID] = newInfo;
                    }
                }
                return true;
            }

            private bool RemoteLocalTarget(long entityID)
            {
                _targetsLocal.Remove(entityID);

                if (AllTargetsExt.ContainsKey(entityID))
                {
                    var original = AllTargetsExt[entityID];
                    if (original.Source == EntitySource.Local)
                    {
                        AllTargetsExt.Remove(entityID);
                    }
                    else if ((original.Source & EntitySource.Local) != 0)
                    {
                        EntitySource newSource = original.Source & ~EntitySource.Local;
                        var newInfo = new EntityInfoExt(original.Info, newSource, original.Relation, original.RelationID);
                        AllTargetsExt[entityID] = newInfo;
                    }
                }
                return true;
            }

            public bool SetTargetRelation(long entityID, EntityRelation relation)
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

                List<long> idsToUpdate = new List<long>()
                {
                    entityID,
                };
                idsToUpdate.AddRange(AllTargetsExt.Values.Where(t => t.RelationID == entityID).Select(t => t.EntityID));

                foreach (var id in idsToUpdate)
                {
                    if (AllTargetsExt.ContainsKey(id))
                    {
                        var original = AllTargetsExt[id];
                        var newInfo = new EntityInfoExt(original.Info, original.Source, relation, original.RelationID);
                        AllTargetsExt[id] = newInfo;
                    }
                    if (_targetsLocal.ContainsKey(id))
                    {
                        var original = _targetsLocal[id];
                        var newInfo = new EntityInfoExt(original.Info, original.Source, relation, original.RelationID);
                        _targetsLocal[id] = newInfo;
                    }
                    if (_targetsRemote.ContainsKey(id))
                    {
                        var original = _targetsRemote[id];
                        var newInfo = new EntityInfoExt(original.Info, original.Source, relation, original.RelationID);
                        _targetsRemote[id] = newInfo;
                    }
                }
                return true;
            }
        }
    }
}
