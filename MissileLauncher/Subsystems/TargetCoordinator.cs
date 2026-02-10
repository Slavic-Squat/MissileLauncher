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
            private double _time;
            private Dictionary<long, EntityInfoExt> _targetsLocal = new Dictionary<long, EntityInfoExt>();
            private List<long> _localsToRemove = new List<long>();
            private Dictionary<long, EntityInfoExt> _targetsRemote = new Dictionary<long, EntityInfoExt>();
            private List<long> _remotesToRemove = new List<long>();
            private Dictionary<long, EntityInfoExt> _allTargetsExt = new Dictionary<long, EntityInfoExt>();
            private HashSet<long> _neutralIDs = new HashSet<long>();
            private HashSet<long> _hostileIDs = new HashSet<long>();
            private HashSet<long> _friendlyIDs = new HashSet<long>();
            private List<long> _idsToUpdate = new List<long>();
            private byte[] _targetsBuffer = new byte[1024];
            private byte[] _selfBuffer = new byte[128];

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
                if (_time == 0)
                {
                    _time = time;
                    return;
                }
                double globalTime = SystemCoordinator.GlobalTime;

                Recieve();

                _localsToRemove.Clear();
                foreach (var targetKey in _targetsLocal.Keys)
                {
                    double timeSinceLastDetection = globalTime - _targetsLocal[targetKey].TimeRecorded;

                    if (timeSinceLastDetection > 5f)
                    {
                        _localsToRemove.Add(targetKey);
                    }
                }

                foreach (var targetKey in _localsToRemove)
                {
                    RemoveLocalTarget(targetKey);
                }

                _remotesToRemove.Clear();
                foreach (var targetKey in _targetsRemote.Keys)
                {
                    double timeSinceLastDetection = globalTime - _targetsRemote[targetKey].TimeRecorded;

                    if (timeSinceLastDetection > 5f)
                    {
                        _remotesToRemove.Add(targetKey);
                    }
                }

                foreach (var targetKey in _remotesToRemove)
                {
                    RemoveRemoteTarget(targetKey);
                }

                Transmit();

                _time = time;
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
                    if (!entityInfo.MissileInfo.IsValid)
                    {
                        return;
                    }
                    relationID = entityInfo.MissileInfo.LauncherID;
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

                _idsToUpdate.Clear();
                _idsToUpdate.Add(entityID);

                foreach (var target in _allTargetsExt.Values)
                {
                    if (target.RelationID == entityID)
                    {
                        _idsToUpdate.Add(target.EntityID);
                    }
                }

                foreach (var id in _idsToUpdate)
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

            private void Transmit()
            {
                int index = 0;
                int count = _targetsLocal.Count;
                if (count == 0)
                {
                    return;
                }

                _targetsBuffer[index++] = (byte)count;

                int sizeIndex;
                int bytesWritten;

                foreach (var target in _targetsLocal.Values)
                {
                    sizeIndex = index++;
                    bytesWritten = target.Info.Serialize(_targetsBuffer, index);
                    index += bytesWritten;
                    _targetsBuffer[sizeIndex] = (byte)bytesWritten;
                }
                if (index > 1)
                {
                    ImmutableArray<byte> bytes = ImmutableArray.Create(_targetsBuffer, 0, index);
                    CommunicationHandler0.SendBroadcast(bytes, "TARGET_SHARE", true);
                }

                index = 0;
                sizeIndex = index++;

                EntityInfo selfInfo = new EntityInfo(SystemCoordinator.SelfID, SystemCoordinator.ReferencePosition, SystemCoordinator.ReferenceVelocity, SystemCoordinator.GlobalTime);
                bytesWritten = selfInfo.Serialize(_selfBuffer, index);
                _selfBuffer[sizeIndex] = (byte)bytesWritten;
                index += bytesWritten;
                if (index > 1)
                {
                    ImmutableArray<byte> bytes = ImmutableArray.Create(_selfBuffer, 0, index);
                    CommunicationHandler0.SendBroadcast(bytes, "FRIENDLY_INFO", true);
                }
            }

            private void Recieve()
            {
                while (CommunicationHandler0.HasMessage("TARGET_SHARE", true))
                {
                    MyIGCMessage message;
                    if (CommunicationHandler0.TryRetrieveMessage("TARGET_SHARE", true, out message))
                    {
                        ImmutableArray<byte> bytes = message.As<ImmutableArray<byte>>();
                        int index = 0;
                        int count = bytes[index++];

                        for (int i = 0; i < count; i++)
                        {
                            byte size = bytes[index++];
                            int bytesRead;
                            EntityInfo entityInfo = EntityInfo.Deserialize(bytes, index, out bytesRead);
                            if (!entityInfo.IsValid || size != bytesRead)
                            {
                                index += size;
                                continue;
                            }
                            index += bytesRead;
                            AddRemoteTarget(entityInfo, false);
                        }
                    }
                }

                while (CommunicationHandler0.HasMessage("FRIENDLY_INFO", true))
                {
                    MyIGCMessage message;
                    if (CommunicationHandler0.TryRetrieveMessage("FRIENDLY_INFO", true, out message))
                    {
                        ImmutableArray<byte> bytes = message.As<ImmutableArray<byte>>();
                        int index = 0;
                        byte size = bytes[index++];
                        int bytesRead;
                        EntityInfo entityInfo = EntityInfo.Deserialize(bytes, index, out bytesRead);
                        if (!entityInfo.IsValid || size != bytesRead)
                        {
                            continue;
                        }
                        AddRemoteTarget(entityInfo, true);
                    }
                }

                while (CommunicationHandler0.HasMessage("ALL_MISSILE_INFO", false))
                {
                    MyIGCMessage message;
                    if (CommunicationHandler0.TryRetrieveMessage("ALL_MISSILE_INFO", false, out message))
                    {
                        ImmutableArray<byte> bytes = message.As<ImmutableArray<byte>>();
                        int index = 0;
                        byte size = bytes[index++];
                        int bytesRead;
                        EntityInfo entityInfo = EntityInfo.Deserialize(bytes, index, out bytesRead);
                        if (!entityInfo.IsValid || size != bytesRead)
                        {
                            continue;
                        }
                        AddRemoteTarget(entityInfo, false);
                    }
                }
            }
        }
    }
}
