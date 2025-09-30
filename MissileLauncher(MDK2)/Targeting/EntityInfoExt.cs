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
            public Source EntitySource { get; private set; }

            public enum Type
            {
                Target, Missile
            }
            public Type EntityType { get; private set; }

            public enum Relation
            {
                Neutral, Hostile, Friendly, Me
            }
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
        }
    }
}
