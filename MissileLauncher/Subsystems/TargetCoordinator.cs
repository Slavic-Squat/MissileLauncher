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
            public double Time { get; private set; }

            private Dictionary<long, EntityInfoExt> _targetsLocal = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, EntityInfoExt> _targetsRemote = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, EntityInfoExt> _allTargetsExt = new Dictionary<long, EntityInfoExt>();
            private HashSet<long> _neutralIDs = new HashSet<long>();
            private HashSet<long> _hostileIDs = new HashSet<long>();
            private HashSet<long> _friendlyIDs = new HashSet<long>();

            public IReadOnlyDictionary<long, EntityInfoExt> AllTargetsExt => _allTargetsExt;

            public TargetCoordinator()
            {
                Init();
            }

            private void Init()
            {
                CommunicationHandler0.RegisterBroadcastListener("TARGET_SHARE", true);
                CommunicationHandler0.RegisterBroadcastListener("ALL_MISSILE_INFO", false);
                CommunicationHandler0.RegisterBroadcastListener("FRIENDLY_INFO", true);
            }

            public void Run(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }
                double globalTime = SystemCoordinator.GlobalTime;

                while (CommunicationHandler0.HasMessage("TARGET_SHARE", true))
                {
                    MyIGCMessage message;
                    if (CommunicationHandler0.TryRetrieveMessage("TARGET_SHARE", true, out message))
                    {
                        byte[] bytes = Convert.FromBase64String(message.Data as string);
                        EntityInfo entityInfo = EntityInfo.Deserialize(bytes, 0);
                        AddRemoteTarget(entityInfo, false);
                    }
                }

                while (CommunicationHandler0.HasMessage("FRIENDLY_INFO", true))
                {
                    MyIGCMessage message;
                    if (CommunicationHandler0.TryRetrieveMessage("FRIENDLY_INFO", true, out message))
                    {
                        byte[] bytes = Convert.FromBase64String(message.Data as string);
                        EntityInfo entityInfo = EntityInfo.Deserialize(bytes, 0);
                        AddRemoteTarget(entityInfo, true);
                    }
                }

                while (CommunicationHandler0.HasMessage("ALL_MISSILE_INFO", false))
                {
                    MyIGCMessage message;
                    if (CommunicationHandler0.TryRetrieveMessage("ALL_MISSILE_INFO", false, out message))
                    {
                        byte[] bytes = Convert.FromBase64String(message.Data as string);
                        EntityInfo entityInfo = EntityInfo.Deserialize(bytes, 0);
                        AddRemoteTarget(entityInfo, false);
                    }
                }

                foreach (var targetKey in _targetsLocal.Keys.ToList())
                {
                    double timeSinceLastDetection = globalTime - _targetsLocal[targetKey].TimeRecorded;

                    if (timeSinceLastDetection > 5f)
                    {
                        RemoveLocalTarget(targetKey);
                        continue;
                    }

                    var targetInfo = _targetsLocal[targetKey].Info;
                    byte[] bytes = targetInfo.Serialize();
                    CommunicationHandler0.SendBroadcast(bytes, "TARGET_SHARE", true);
                }

                foreach (var targetKey in _targetsRemote.Keys.ToList())
                {
                    double timeSinceLastDetection = globalTime - _targetsRemote[targetKey].TimeRecorded;

                    if (timeSinceLastDetection > 5f)
                    {
                        RemoveRemoteTarget(targetKey);
                    }
                }
                Time = time;
            }

            private void AddRemoteTarget(EntityInfo entityInfo, bool friendly)
            {
                if (!entityInfo.IsValid)
                {
                    return;
                }
                var entityID = entityInfo.EntityID;
                var relationID = entityID;

                if (entityInfo.Type == EntityType.Missile)
                {
                    if (entityInfo.SubType != EntityInfoSubType.MissileInfoLite)
                    {
                        return;
                    }

                    relationID = entityInfo.MissileInfoLite.Value.LauncherID;
                }

                if (entityID == SystemCoordinator.SelfID || relationID == SystemCoordinator.SelfID)
                {
                    return;
                }

                if (friendly)
                {
                    SetTargetRelation(relationID, EntityRelation.Friendly);
                }
                else if (!_neutralIDs.Contains(relationID) && !_hostileIDs.Contains(relationID) && !_friendlyIDs.Contains(relationID))
                {
                    SetTargetRelation(relationID, EntityRelation.Neutral);
                }

                EntitySource source = EntitySource.Remote;
                EntityRelation relation = _friendlyIDs.Contains(relationID) ? EntityRelation.Friendly : _hostileIDs.Contains(relationID) ? EntityRelation.Hostile : EntityRelation.Neutral;

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

                if (!_allTargetsExt.ContainsKey(entityID))
                {
                    _allTargetsExt.Add(entityID, entityInfoExt);
                }
                else
                {
                    var original = _allTargetsExt[entityID];
                    _allTargetsExt[entityID] = original.Merge(entityInfoExt);
                }
            }

            public void AddLocalTarget(EntityInfoExt targetInfoExt)
            {
                if (!targetInfoExt.IsValid || targetInfoExt.Source != EntitySource.Local)
                {
                    return;
                }
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

                if (!_allTargetsExt.ContainsKey(entityID))
                {
                    _allTargetsExt.Add(entityID, targetInfoExt);
                }
                else
                {
                    var original = _allTargetsExt[entityID];
                    _allTargetsExt[entityID] = original.Merge(targetInfoExt);
                }
            }

            private void RemoveRemoteTarget(long entityID)
            {
                _targetsRemote.Remove(entityID);

                if (_allTargetsExt.ContainsKey(entityID))
                {
                    var original = _allTargetsExt[entityID];
                    if (original.Source == EntitySource.Remote)
                    {
                        _allTargetsExt.Remove(entityID);
                    }
                    else if ((original.Source & EntitySource.Remote) != 0)
                    {
                        EntitySource newSource = original.Source & ~EntitySource.Remote;
                        var newInfo = new EntityInfoExt(original.Info, newSource, original.Relation, original.RelationID);
                        _allTargetsExt[entityID] = newInfo;
                    }
                    else
                    {
                        _allTargetsExt.Remove(entityID);
                    }
                }
            }

            private void RemoveLocalTarget(long entityID)
            {
                _targetsLocal.Remove(entityID);

                if (_allTargetsExt.ContainsKey(entityID))
                {
                    var original = _allTargetsExt[entityID];
                    if (original.Source == EntitySource.Local)
                    {
                        _allTargetsExt.Remove(entityID);
                    }
                    else if ((original.Source & EntitySource.Local) != 0)
                    {
                        EntitySource newSource = original.Source & ~EntitySource.Local;
                        var newInfo = new EntityInfoExt(original.Info, newSource, original.Relation, original.RelationID);
                        _allTargetsExt[entityID] = newInfo;
                    }
                    else
                    {
                        _allTargetsExt.Remove(entityID);
                    }
                }
            }

            public void SetTargetRelation(long entityID, EntityRelation relation)
            {
                switch (relation)
                {
                    case EntityRelation.Neutral:
                        _hostileIDs.Remove(entityID);
                        _friendlyIDs.Remove(entityID);
                        _neutralIDs.Add(entityID);
                        break;

                    case EntityRelation.Friendly:
                        _neutralIDs.Remove(entityID);
                        _hostileIDs.Remove(entityID);
                        _friendlyIDs.Add(entityID);
                        break;

                    case EntityRelation.Hostile:
                        _neutralIDs.Remove(entityID);
                        _friendlyIDs.Remove(entityID);
                        _hostileIDs.Add(entityID);
                        break;
                    case EntityRelation.Me:
                        _neutralIDs.Remove(entityID);
                        _friendlyIDs.Remove(entityID);
                        _hostileIDs.Remove(entityID);
                        break;
                }

                List<long> idsToUpdate = new List<long>()
                {
                    entityID,
                };
                idsToUpdate.AddRange(_allTargetsExt.Values.Where(t => t.RelationID == entityID).Select(t => t.EntityID));

                foreach (var id in idsToUpdate)
                {
                    if (_allTargetsExt.ContainsKey(id))
                    {
                        var original = _allTargetsExt[id];
                        var newInfo = new EntityInfoExt(original.Info, original.Source, relation, original.RelationID);
                        _allTargetsExt[id] = newInfo;
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
            }
        }
    }
}
