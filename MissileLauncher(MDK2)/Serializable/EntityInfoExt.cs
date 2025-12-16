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
            public double TimeRecorded => Info.TimeRecorded;
            public EntityType Type => Info.Type;
            public EntityInfoSubType SubType => Info.SubType;
            public EntitySource Source { get; private set; }
            public EntityRelation Relation { get; private set; }
            public long RelationID { get; private set; }
            public bool IsValid { get; private set; }

            public EntityInfoExt(EntityInfo info, EntitySource source, EntityRelation relation, long relationID)
            {
                Info = info;
                Source = source;
                Relation = relation;
                RelationID = relationID;
                IsValid = true;
            }

            public EntityInfoExt(MyDetectedEntityInfo entityInfo, double timeRecorded)
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
                RelationID = entityInfo.EntityId;
                IsValid = true;
            }

            public EntityInfoExt Merge(EntityInfoExt other)
            {
                if (EntityID != other.EntityID)
                {
                    return this;
                }
                if (Type == EntityType.Target && other.Type == EntityType.Missile)
                {
                    RelationID = other.RelationID;
                }
                Info = Info.Merge(other.Info);
                Relation = other.Relation;
                Source |= other.Source;
                return this;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder($"[{GetDisplayString(Type)} INFO]\n-----------------------\nTYPE: {GetDisplayString(Type)}\nSRC: {GetDisplayString(Source)}\nREL: {GetDisplayString(Relation)}\n");

                float distance = Vector3.Distance(SystemCoordinator.ReferencePosition, Position);
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

                float age = (float)(SystemCoordinator.GlobalTime - Info.TimeRecorded) * 1000;
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
                    sb.Append($"MISL TYPE: {GetDisplayString(missileInfo.Type)}\nPAYLOAD: {GetDisplayString(missileInfo.Payload)}\nSTAGE: {GetDisplayString(missileInfo.Stage)}\n");
                }

                return sb.ToString().TrimEnd('\n');
            }
        }
    }
}
