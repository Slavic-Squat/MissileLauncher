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
            private IMyBroadcastListener _missilesInfoListener;
            #endregion

            #region Fields
            private Dictionary<long, MyTuple<string, long, Vector3, Vector3, long>> _targetsIGC = new Dictionary<long, MyTuple<string, long, Vector3, Vector3, long>>();
            #endregion

            #region Properties
            public Program Program { get; private set; }
            public int ID { get; private set; }
            public string Name { get; private set; }
            public string CoordinatorTag { get; private set; }
            public EntityInfo Launcher {  get; private set; }
            public Dictionary<long, EntityInfo> Entities {  get; private set; }
            public HashSet<long> NeutralTargetIDs { get; private set; }
            public HashSet<long> HostileTargetIDs { get; private set; }
            public HashSet<long> FriendlyTargetIDs { get; private set; }
            public Dictionary<long, HashSet<long>> MissileRelationMap {  get; private set; }
            #endregion

            public TargetCoordinator(IMyCubeGrid referenceGrid, string coordinatorTag)
            {
                _referenceGrid = referenceGrid;
                Name = _referenceGrid.Name;
                CoordinatorTag = coordinatorTag;

                Launcher = new EntityInfo();
                Entities = new Dictionary<long, EntityInfo>();


                _missilesInfoListener = Program.IGC.RegisterBroadcastListener($"[{CoordinatorTag}]_MissilesInfo");
            }

            public void Run(DateTime time)
            {
                while (_missilesInfoListener.HasPendingMessage)
                {
                    var messageIn = _missilesInfoListener.AcceptMessage();
                    if (messageIn.Data is MyTuple<MyTuple<long, Vector3, Vector3, long, string>, MyTuple<long, long, string>>)
                    {
                        var missileInfo = messageIn.As<MyTuple<MyTuple<long, Vector3, Vector3, long, string>, MyTuple<long, long, string>>>();
                        AddEntity(MissileInfo.CreateFromIGC(missileInfo));
                    }
                }

                for (int i = MissileIDs.Count - 1; i >= 0; i--)
                {
                    var missileID = MissileIDs[i];
                    TimeSpan timeSinceLastUpdate = time - Missiles[missileID].TimeRecorded;

                    if (timeSinceLastUpdate.TotalSeconds > 5)
                    {
                        RemoveMissile(missileID);
                    }
                }
                for (int i = TargetIDs.Count - 1; i >= 0; i--)
                {
                    var targetID = TargetIDs[i];
                    TimeSpan timeSinceLastDetection = time - Targets[targetID].TimeRecorded;

                    if (timeSinceLastDetection.TotalSeconds > 5)
                    {
                        RemoveTarget(targetID);
                    }
                }

                Launcher.Name = _referenceGrid.Name;
                Launcher.EntityID = _referenceGrid.EntityId;
                Launcher.Position = _referenceGrid.GetPosition();
                Launcher.Velocity = _referenceGrid.LinearVelocity;

                _targetsIGC.Clear();
                foreach (var target in Targets)
                {
                    _targetsIGC.Add(target.Key, TargetInfo.ToIGC(target.Value));
                }
                var messageOut0 = _targetsIGC.ToImmutableDictionary();
                var messageOut1 = EntityInfo.ToIGC(Launcher);
                Program.IGC.SendBroadcastMessage($"[{CoordinatorTag}]_TargetsInfo", messageOut0);
                Program.IGC.SendBroadcastMessage($"[{CoordinatorTag}]_LauncherInfo", messageOut1);
            }

            public void AddEntity(EntityInfo entity)
            {
                var entityID = entity.EntityID;
                var relationIDKey = entityID;

                if (entity is MissileInfo)
                {
                    var missile = entity as MissileInfo;
                    var missileID = entity.EntityID;
                    var launcherID = missile.LauncherID;
                    relationIDKey = launcherID;

                    if (!MissileRelationMap.ContainsKey(launcherID))
                    {
                        MissileRelationMap.Add(launcherID, new HashSet<long> { missileID });
                    }
                    else if (!MissileRelationMap[launcherID].Contains(missileID))
                    {
                        MissileRelationMap[launcherID].Add(missileID);
                    }
                }

                if (NeutralTargetIDs.Contains(relationIDKey))
                {
                    entity.Relation = EntityInfo.EntityRelation.Neutral;
                }
                else if (HostileTargetIDs.Contains(relationIDKey))
                {
                    entity.Relation = EntityInfo.EntityRelation.Hostile;
                }
                else if (FriendlyTargetIDs.Contains(relationIDKey))
                {
                    entity.Relation = EntityInfo.EntityRelation.Friendly;
                }
                else
                {
                    entity.Relation = EntityInfo.EntityRelation.Unknown;
                }

                if (!Entities.ContainsKey(entityID))
                {
                    Entities.Add(entityID, entity);
                }
                else if (Entities[entityID].TimeRecorded < entity.TimeRecorded)
                {
                    var storedEntity = Entities[entityID];
                    storedEntity.TimeRecorded = entity.TimeRecorded;
                    storedEntity.Position = entity.Position;
                    storedEntity.Velocity = entity.Velocity;
                }
            }

            public void RemoveEntity(long entityID)
            {
                EntityInfo entity = null;
                if (Entities.ContainsKey(entityID))
                {
                    entity = Entities[entityID];
                    Entities.Remove(entityID);
                }

                if (entity is MissileInfo)
                {
                    var missile = entity as MissileInfo;
                    var missileID = missile.EntityID;
                    var launcherID = missile.LauncherID;

                    if (MissileRelationMap.ContainsKey(launcherID))
                    {
                        MissileRelationMap[launcherID].Remove(missileID);
                    }                    
                }
                else
                {
                    MissileRelationMap.Remove(entityID);
                }
            }

            public void SetTargetRelation(long entityID, EntityInfo.EntityRelation relation)
            {
                if (!Entities.ContainsKey(entityID))
                {
                    return;
                }

                var entity = Entities[entityID];
                entity.Relation = relation;

                switch (relation)
                {
                    case EntityInfo.EntityRelation.Neutral:
                        HostileTargetIDs.Remove(entityID);
                        FriendlyTargetIDs.Remove(entityID);

                        if (!NeutralTargetIDs.Contains(entityID))
                        {
                            NeutralTargetIDs.Add(entityID);
                        }
                        break;

                    case EntityInfo.EntityRelation.Friendly:
                        NeutralTargetIDs.Remove(entityID);
                        HostileTargetIDs.Remove(entityID);

                        if (!FriendlyTargetIDs.Contains(entityID))
                        {
                            FriendlyTargetIDs.Add(entityID);
                        }
                        break;

                    case EntityInfo.EntityRelation.Hostile:
                        NeutralTargetIDs.Remove(entityID);
                        FriendlyTargetIDs.Remove(entityID);

                        if (!HostileTargetIDs.Contains(entityID))
                        {
                            HostileTargetIDs.Add(entityID);
                        }
                        break;
                }
            }
        }
    }
}
