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
            private IMyCubeGrid _referenceGrid;
            private CommunicationHandler _communicationHandler;
            #endregion

            #region Fields

            #endregion

            #region Properties
            public int ID { get; private set; }
            public Dictionary<long, EntityInfo> EntitiesLocal { get; private set; }
            public Dictionary<long, EntityInfo> EntitiesRemote { get; private set; }
            public HashSet<long> NeutralIDs { get; private set; }
            public HashSet<long> HostileIDs { get; private set; }
            public HashSet<long> FriendlyIDs { get; private set; }
            #endregion

            public enum TargetRelation : byte
            {
                Neutral, Friendly, Hostile
            }

            public TargetCoordinator(IMyCubeGrid referenceGrid, CommunicationHandler communicationHandler)
            {
                _referenceGrid = referenceGrid;
                _communicationHandler = communicationHandler;
                _communicationHandler.RegisterBroadcastListener("EntityInfo");

                EntitiesLocal = new Dictionary<long, EntityInfo>();
                EntitiesRemote = new Dictionary<long, EntityInfo>();
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

                foreach (var entityKVP in EntitiesLocal)
                {
                    TimeSpan timeSinceLastDetection = time - entityKVP.Value.TimeRecorded;

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveEntity(entityKVP.Key, false);
                    }
                }

                foreach (var entityKVP in EntitiesRemote)
                {
                    TimeSpan timeSinceLastDetection = time - entityKVP.Value.TimeRecorded;

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveEntity(entityKVP.Key, true);
                    }
                }

                foreach (var entity in EntitiesLocal.Values)
                {
                    byte[] data = entity.Serialize();
                    _communicationHandler.SendBroadcast(data, "EntityInfo");
                }
            }

            public void AddEntity(EntityInfo entity, bool remote)
            {
                var entityID = entity.EntityID;

                var dictionary = remote ? EntitiesRemote : EntitiesLocal;

                if (!dictionary.ContainsKey(entityID))
                {
                    dictionary.Add(entityID, entity);
                }
                else if (dictionary[entityID].TimeRecorded < entity.TimeRecorded)
                {
                    var storedEntity = dictionary[entityID];
                    storedEntity.UpdateFromEntityInfo(entity);
                }
            }

            public void RemoveEntity(long entityID, bool remote)
            {
                var dictionary = remote ? EntitiesRemote : EntitiesLocal;
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

            public Dictionary<long, EntityInfo> GetAllEntities()
            {
                var allEntities = new Dictionary<long, EntityInfo>(EntitiesLocal);

                foreach (var entityKVP in EntitiesRemote)
                {
                    if (!allEntities.ContainsKey(entityKVP.Key))
                    {
                        allEntities.Add(entityKVP.Key, entityKVP.Value);
                    }
                    else if (allEntities[entityKVP.Key].TimeRecorded < entityKVP.Value.TimeRecorded)
                    {
                        allEntities[entityKVP.Key] = entityKVP.Value;
                    }
                }

                return allEntities;
            }
        }
    }
}
