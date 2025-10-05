using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
        public struct EntityInfoExt
        {
            public IEntityInfo Info { get; set; }
            public long EntityID => Info.EntityID;
            public Vector3 Position => Info.Position;
            public Vector3 Velocity => Info.Velocity;
            public DateTime TimeRecorded => Info.TimeRecorded;
            public EntitySource Source { get; set; }
            public EntityType Type { get; set; }
            public EntityRelation Relation { get; set; }
            public bool IsEmpty => Info == null;

            public EntityInfoExt(IEntityInfo info, EntitySource source, EntityRelation relation)
            {
                Info = info;
                if (info is MissileInfoLite || info is MissileInfo)
                {
                    Type = EntityType.Missile;
                }
                else
                {
                    Type = EntityType.Target;
                }
                Source = source;
                Relation = relation;
            }

            public EntityInfoExt(MyDetectedEntityInfo entityInfo, DateTime timeRecorded)
            {
                Info = new TargetInfo(entityInfo, timeRecorded);
                Type = EntityType.Target;
                Source = EntitySource.Local;

                MyRelationsBetweenPlayerAndBlock raycastRelation = entityInfo.Relationship;

                switch (raycastRelation)
                {
                    case MyRelationsBetweenPlayerAndBlock.Enemies:
                        Relation = EntityRelation.Hostile;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Neutral:
                        Relation = EntityRelation.Neutral;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Friends:
                        Relation = EntityRelation.Friendly;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Owner:
                        Relation = EntityRelation.Me;
                        break;
                    default:
                        Relation = EntityRelation.Neutral;
                        break;
                }
            }

            public void UpdateFromRaycast(MyDetectedEntityInfo entityInfo, DateTime timeRecorded)
            {
                Info.UpdateFromRaycast(entityInfo, timeRecorded);
                Source |= EntitySource.Local;

                MyRelationsBetweenPlayerAndBlock raycastRelation = entityInfo.Relationship;

                switch (raycastRelation)
                {
                    case MyRelationsBetweenPlayerAndBlock.Enemies:
                        Relation = EntityRelation.Hostile;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Neutral:
                        Relation = EntityRelation.Neutral;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Friends:
                        Relation = EntityRelation.Friendly;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Owner:
                        Relation = EntityRelation.Me;
                        break;
                    default:
                        break;
                }
            }

            public void Merge(EntityInfoExt other)
            {
                if (EntityID != other.EntityID)
                {
                    return;
                }
                if (other.Type > Type)
                {
                    Type = other.Type;
                    other.Info.UpdateFromEntityInfo(Info);
                    Info = other.Info;

                    Relation = other.Relation;
                }
                else
                {
                    Info.UpdateFromEntityInfo(other.Info);
                }
                Source |= other.Source;
            }

            public string ToString(Vector3 referencePos, DateTime time)
            {
                StringBuilder sb = new StringBuilder($"INFO:\n-----------------------\nTYPE: {GetName(Type)}\nSRC: {GetName(Source)}\nREL: {GetName(Relation)}\n");

                float distance = Vector3.Distance(referencePos, Position);
                if (distance > 1000f)
                {
                    distance /= 1000f;
                    sb.Append($"DIST: {distance:0.0} km\n");
                }
                else
                {
                    sb.Append($"DIST: {distance:0} m\n");
                }

                float speed = Info.Velocity.Length();
                if (speed > 1000f)
                {
                    speed /= 1000f;
                    sb.Append($"SPD: {speed:0.0} km/s\n");
                }
                else
                {
                    sb.Append($"SPD: {speed:0} m/s\n");
                }

                float age = (float)(time - Info.TimeRecorded).TotalMilliseconds;
                if (age > 1000f)
                {
                    age /= 1000f;
                    sb.Append($"AGE: {age:0.0} s\n");
                }
                else
                {
                    sb.Append($"AGE: {age:0} ms\n");
                }

                return sb.ToString().TrimEnd('\n');
            }
        }
    }
}
