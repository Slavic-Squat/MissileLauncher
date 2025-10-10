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
            public EntityInfo Info { get; private set; }
            public long EntityID => Info.EntityID;
            public Vector3 Position => Info.Position;
            public Vector3 Velocity => Info.Velocity;
            public DateTime TimeRecorded => Info.TimeRecorded;
            public EntityType Type => Info.Type;
            public EntityInfoSubType SubType => Info.SubType;
            public EntitySource Source { get; private set; }
            public EntityRelation Relation { get; private set; }
            public bool IsValid { get; private set; }

            public EntityInfoExt(EntityInfo info, EntitySource source, EntityRelation relation)
            {
                Info = info;
                Source = source;
                Relation = relation;
                IsValid = true;
            }

            public EntityInfoExt(MyDetectedEntityInfo entityInfo, DateTime timeRecorded)
            {
                Info = new EntityInfo(entityInfo, timeRecorded);
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
                IsValid = true;
            }

            public EntityInfoExt Merge(EntityInfoExt other)
            {
                if (EntityID != other.EntityID)
                {
                    return this;
                }
                if (other.TimeRecorded > TimeRecorded)
                {
                    Relation = other.Relation;
                }
                Info = Info.Merge(other.Info);
                Source |= other.Source;
                return this;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder($"INFO:\n-----------------------\nTYPE: {GetName(Type)}\nSRC: {GetName(Source)}\nREL: {GetName(Relation)}\n");

                float distance = Vector3.Distance(SystemCoordinator.ReferenceBasis.Translation, Position);
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

                float age = (float)(SystemCoordinator.SystemTime - Info.TimeRecorded).TotalMilliseconds;
                if (age > 1000f)
                {
                    age /= 1000f;
                    sb.Append($"AGE: {age:0.0} s\n");
                }
                else
                {
                    sb.Append($"AGE: {age:0} ms\n");
                }

                if (Info.SubType == EntityInfoSubType.MissileInfo)
                {
                    var missileInfo = Info.MissileInfo.Value;
                    sb.Append($"MISL TYPE: {GetName(missileInfo.Type)}\nPAYLOAD: {GetName(missileInfo.Payload)}\nSTAGE: {GetName(missileInfo.Stage)}\n");
                }

                return sb.ToString().TrimEnd('\n');
            }
        }
    }
}
