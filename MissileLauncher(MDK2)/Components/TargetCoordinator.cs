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
            public Dictionary<long, EntityInfo> TargetsLocal { get; private set; }
            public Dictionary<long, EntityInfo> TargetsRemote { get; private set; }
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
                _communicationHandler.RegisterBroadcastListener("EntityInfo");

                TargetsLocal = new Dictionary<long, EntityInfo>();
                TargetsRemote = new Dictionary<long, EntityInfo>();
                NeutralIDs = new HashSet<long>();
                HostileIDs = new HashSet<long>();
                FriendlyIDs = new HashSet<long>();
            }

            public void Run(DateTime time)
            {
                MyIGCMessage message;
                if (_communicationHandler.TryRetrieveMessage("EntityInfo", out message))
                {
                    object messageObject = Deserializer.Deserialize(message.Data as string);
                    if (messageObject is EntityInfo)
                    {
                        AddEntity(messageObject as EntityInfo, true);
                    }
                }

                foreach (var entityKVP in TargetsLocal)
                {
                    TimeSpan timeSinceLastDetection = time - entityKVP.Value.TimeRecorded;

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveEntity(entityKVP.Key, false);
                    }
                }

                foreach (var entityKVP in TargetsRemote)
                {
                    TimeSpan timeSinceLastDetection = time - entityKVP.Value.TimeRecorded;

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveEntity(entityKVP.Key, true);
                    }
                }

                foreach (var entity in TargetsLocal.Values)
                {
                    byte[] data = entity.Serialize();
                    _communicationHandler.SendBroadcast(data, "EntityInfo");
                }
            }

            public void AddEntity(EntityInfo entityInfo, bool remote)
            {
                var entityID = entityInfo.EntityID;
                var relationID = entityID;

                if (entityID == _selfID)
                {
                    return;
                }
                else if (entityInfo is MissileInfo)
                {
                    var missile = entityInfo as MissileInfo;
                    relationID = missile.LauncherID;
                    if (missile.LauncherID == _selfID)
                    {
                        return;
                    }
                }

                if (!NeutralIDs.Contains(relationID) && !HostileIDs.Contains(relationID) && !FriendlyIDs.Contains(relationID))
                {
                    NeutralIDs.Add(relationID);
                }

                var dictionary = remote ? TargetsRemote : TargetsLocal;

                if (!dictionary.ContainsKey(entityID))
                {
                    dictionary.Add(entityID, entityInfo);
                }
                else if (dictionary[entityID].TimeRecorded < entityInfo.TimeRecorded)
                {
                    var storedEntity = dictionary[entityID];
                    storedEntity.UpdateFromEntityInfo(entityInfo);
                }
            }

            public void RemoveEntity(long entityID, bool remote)
            {
                var dictionary = remote ? TargetsRemote : TargetsLocal;
                if (dictionary.ContainsKey(entityID))
                {
                    dictionary.Remove(entityID);
                }
            }

            public void SetTargetRelation(long entityID, TargetRelation relation)
            {
                switch (relation)
                {
                    case TargetRelation.Neutral:
                        HostileIDs.Remove(entityID);
                        FriendlyIDs.Remove(entityID);

                        if (!NeutralIDs.Contains(entityID))
                        {
                            NeutralIDs.Add(entityID);
                        }
                        break;

                    case TargetRelation.Friendly:
                        NeutralIDs.Remove(entityID);
                        HostileIDs.Remove(entityID);

                        if (!FriendlyIDs.Contains(entityID))
                        {
                            FriendlyIDs.Add(entityID);
                        }
                        break;

                    case TargetRelation.Hostile:
                        NeutralIDs.Remove(entityID);
                        FriendlyIDs.Remove(entityID);

                        if (!HostileIDs.Contains(entityID))
                        {
                            HostileIDs.Add(entityID);
                        }
                        break;
                }
            }

            public Dictionary<long, EntityInfoExt> GetAllTargets()
            {
                var allEntityInfos = new Dictionary<long, EntityInfo>(TargetsLocal);

                foreach (var entityInfoKVP in TargetsRemote)
                {
                    if (!allEntityInfos.ContainsKey(entityInfoKVP.Key))
                    {
                        allEntityInfos.Add(entityInfoKVP.Key, entityInfoKVP.Value);
                    }
                    else if (allEntityInfos[entityInfoKVP.Key].TimeRecorded < entityInfoKVP.Value.TimeRecorded)
                    {
                        allEntityInfos[entityInfoKVP.Key] = entityInfoKVP.Value;
                    }
                }

                var allEntityInfosExt = new Dictionary<long, EntityInfoExt>();

                foreach (var entityInfoExtKVP in allEntityInfos)
                {
                    long key = entityInfoExtKVP.Key;
                    long sourceKey = key;
                    EntityInfo entityInfo = entityInfoExtKVP.Value;
                    long relationKey = key;

                    float distance = Vector3.Distance(entityInfo.Position, _referenceBlock.GetPosition());

                    if (entityInfo is MissileInfo)
                    {
                        MissileInfo missileInfo = entityInfo as MissileInfo;
                        relationKey = missileInfo.LauncherID;
                    }

                    EntityInfoExt.Source source = EntityInfoExt.Source.None;
                    EntityInfoExt.Relation relation;

                    if (TargetsLocal.ContainsKey(key))
                    {
                        source |= EntityInfoExt.Source.Local;
                    }

                    if (TargetsRemote.ContainsKey(key))
                    {
                        source |= EntityInfoExt.Source.Remote;
                    }

                    if (NeutralIDs.Contains(key))
                    {
                        relation = EntityInfoExt.Relation.Neutral;
                    }
                    else if (FriendlyIDs.Contains(key))
                    {
                        relation = EntityInfoExt.Relation.Friendly;
                    }
                    else if (HostileIDs.Contains(key))
                    {
                        relation = EntityInfoExt.Relation.Hostile;
                    }
                    else if (relationKey == _selfID)
                    {
                        relation = EntityInfoExt.Relation.Me;
                    }
                    else
                    {
                        relation = EntityInfoExt.Relation.Neutral;
                    }

                    var entityInfoExt = new EntityInfoExt(entityInfo, source, relation, distance);
                    allEntityInfosExt.Add(key, entityInfoExt);
                }

                return allEntityInfosExt;
            }
        }
    }
}
