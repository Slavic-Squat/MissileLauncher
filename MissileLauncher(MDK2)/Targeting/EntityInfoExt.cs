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

            [Flags]
            public enum Source
            {
                None = 0, Local = 1, Remote = 1 << 1, Both = Local | Remote
            }
            public static Dictionary<Source, string> SourceNames = new Dictionary<Source, string>()
            {
                { Source.None, "None" },
                { Source.Local, "Local" },
                { Source.Remote, "Remote" },
                { Source.Both, "Remote + Local" }
            };
            public Source EntitySource { get; private set; }

            public enum Type
            {
                Target, Missile
            }
            public static Dictionary<Type, string> TypeNames = new Dictionary<Type, string>()
            {
                { Type.Target, "Target" },
                { Type.Missile, "Missile" }
            };
            public Type EntityType { get; private set; }

            public enum Relation
            {
                Neutral, Hostile, Friendly, Me
            }
            public static Dictionary<Relation, string> RelationNames = new Dictionary<Relation, string>()
            {
                { Relation.Neutral, "Neutral" },
                { Relation.Hostile, "Hostile" },
                { Relation.Friendly, "Friendly" },
                { Relation.Me, "Me" }
            };
            public Relation EntityRelation { get; private set; }

            public float Distance { get; private set; }

            public EntityInfoExt(EntityInfo info, Source source, Relation relation, float distance)
            {
                Info = info;
                EntitySource = source;
                EntityType = info is MissileInfoLite ? Type.Missile : Type.Target;
                EntityRelation = relation;
                Distance = distance;
            }

            public string ToString(DateTime time)
            {
                StringBuilder sb = new StringBuilder($"Entity Info:\nType: {TypeNames[EntityType]}\nSource: {SourceNames[EntitySource]}\nRelation: {RelationNames[EntityRelation]}\n");

                float distance = Distance;
                if (distance > 1000f)
                {
                    distance /= 1000f;
                    sb.Append($"Distance: {distance:0.0} km\n");
                }
                else
                {
                    sb.Append($"Distance: {distance:0} m\n");
                }

                float speed = Info.Velocity.Length();
                if (speed > 1000f)
                {
                    speed /= 1000f;
                    sb.Append($"Speed: {speed:0.0} km/s\n");
                }
                else
                {
                    sb.Append($"Speed: {speed:0} m/s\n");
                }

                float age = (float)(time - Info.TimeRecorded).TotalMilliseconds;
                if (age > 1000f)
                {
                    age /= 1000f;
                    sb.Append($"Age: {age:0.0} s\n");
                }
                else
                {
                    sb.Append($"Age: {age:0} ms\n");
                }

                return sb.ToString().TrimEnd('\n');
            }
        }
    }
}
