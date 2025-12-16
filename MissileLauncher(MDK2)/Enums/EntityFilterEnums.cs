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
        [Flags]
        public enum EntityTypeFilter : byte
        {
            None = 0, Targets = 1 << 0, Missiles = 1 << 1, All = Targets | Missiles,
        }

        [Flags]
        public enum EntityRelationFilter : byte
        {
            None = 0, Hostile = 1 << 0, Neutral = 1 << 1, Friendly = 1 << 2, Me = 1 << 3, All = Hostile | Neutral | Friendly | Me,
        }

        [Flags]
        public enum EntitySourceFilter : byte
        {
            None = 0, Local = 1 << 0, Remote = 1 << 1, Both = Local | Remote
        }
        public static class EntityFilterEnumHelper
        {
            public static string GetDisplayString(EntityTypeFilter filter)
            {
                switch (filter)
                {
                    case EntityTypeFilter.None: return "NONE";
                    case EntityTypeFilter.Targets: return "TRGT";
                    case EntityTypeFilter.Missiles: return "MISL";
                    case EntityTypeFilter.All: return "ALL";
                    default: return "N/A";
                }
            }

            public static string GetDisplayString(EntityRelationFilter filter)
            {
                switch (filter)
                {
                    case EntityRelationFilter.None: return "NONE";
                    case EntityRelationFilter.Hostile: return "HSTL";
                    case EntityRelationFilter.Neutral: return "NTRL";
                    case EntityRelationFilter.Friendly: return "FRND";
                    case EntityRelationFilter.Me: return "ME";
                    case EntityRelationFilter.All: return "ALL";
                    default: return "N/A";
                }
            }

            public static string GetDisplayString(EntitySourceFilter filter)
            {
                switch (filter)
                {
                    case EntitySourceFilter.None: return "NONE";
                    case EntitySourceFilter.Local: return "LOCAL";
                    case EntitySourceFilter.Remote: return "REMOTE";
                    case EntitySourceFilter.Both: return "BOTH";
                    default: return "N/A";
                }
            }

            private static readonly EntityTypeFilter[] EntityTypeFilterCycles = new EntityTypeFilter[] { EntityTypeFilter.Targets, EntityTypeFilter.Missiles, EntityTypeFilter.All };

            public static EntityTypeFilter NextEntityTypeFilter(EntityTypeFilter filter)
            {
                int index = Array.IndexOf(EntityTypeFilterCycles, filter);
                if (index < 0) return EntityTypeFilterCycles[0];
                index = (index + 1) % EntityTypeFilterCycles.Length;
                return EntityTypeFilterCycles[index];
            }

            public static EntityTypeFilter PreviousEntityTypeFilter(EntityTypeFilter filter)
            {
                int index = Array.IndexOf(EntityTypeFilterCycles, filter);
                if (index < 0) return EntityTypeFilterCycles[0];
                index = (index - 1 + EntityTypeFilterCycles.Length) % EntityTypeFilterCycles.Length;
                return EntityTypeFilterCycles[index];
            }

            

            private static readonly EntityRelationFilter[] EntityRelationFilterCycles = new EntityRelationFilter[] { EntityRelationFilter.Hostile, EntityRelationFilter.Neutral, EntityRelationFilter.Friendly, EntityRelationFilter.Me, EntityRelationFilter.All };

            public static EntityRelationFilter NextEntityRelationFilter(EntityRelationFilter filter)
            {
                int index = Array.IndexOf(EntityRelationFilterCycles, filter);
                if (index < 0) return EntityRelationFilterCycles[0];
                index = (index + 1) % EntityRelationFilterCycles.Length;
                return EntityRelationFilterCycles[index];
            }

            public static EntityRelationFilter PreviousEntityRelationFilter(EntityRelationFilter filter)
            {
                int index = Array.IndexOf(EntityRelationFilterCycles, filter);
                if (index < 0) return EntityRelationFilterCycles[0];
                index = (index - 1 + EntityRelationFilterCycles.Length) % EntityRelationFilterCycles.Length;
                return EntityRelationFilterCycles[index];
            }

            

            private static readonly EntitySourceFilter[] EntitySourceFilterCycles = new EntitySourceFilter[] { EntitySourceFilter.Local, EntitySourceFilter.Remote, EntitySourceFilter.Both };

            public static EntitySourceFilter NextEntitySourceFilter(EntitySourceFilter filter)
            {
                int index = Array.IndexOf(EntitySourceFilterCycles, filter);
                if (index < 0) return EntitySourceFilterCycles[0];
                index = (index + 1) % EntitySourceFilterCycles.Length;
                return EntitySourceFilterCycles[index];
            }

            public static EntitySourceFilter PreviousEntitySourceFilter(EntitySourceFilter filter)
            {
                int index = Array.IndexOf(EntitySourceFilterCycles, filter);
                if (index < 0) return EntitySourceFilterCycles[0];
                index = (index - 1 + EntitySourceFilterCycles.Length) % EntitySourceFilterCycles.Length;
                return EntitySourceFilterCycles[index];
            }

            private static EntityTypeFilter ToMask(EntityType type)
            {
                switch (type)
                {
                    case EntityType.Target: return EntityTypeFilter.Targets;
                    case EntityType.Missile: return EntityTypeFilter.Missiles;
                    default: return EntityTypeFilter.None;
                }
            }

            private static EntityRelationFilter ToMask(EntityRelation relation)
            {
                switch (relation)
                {
                    case EntityRelation.Neutral: return EntityRelationFilter.Neutral;
                    case EntityRelation.Hostile: return EntityRelationFilter.Hostile;
                    case EntityRelation.Friendly: return EntityRelationFilter.Friendly;
                    case EntityRelation.Me: return EntityRelationFilter.Me;
                    default: return EntityRelationFilter.None;
                }
            }

            private static EntitySourceFilter ToMask(EntitySource source)
            {
                switch (source)
                {
                    case EntitySource.None: return EntitySourceFilter.None;
                    case EntitySource.Local: return EntitySourceFilter.Local;
                    case EntitySource.Remote: return EntitySourceFilter.Remote;
                    case EntitySource.Both: return EntitySourceFilter.Both;
                    default: return EntitySourceFilter.None;
                }
            }

            public static bool Matches(EntityInfoExt entityInfoExt, EntityTypeFilter typeFilter, EntityRelationFilter relationFilter, EntitySourceFilter sourceFilter)
            {
                if ((typeFilter & ToMask(entityInfoExt.Type)) == 0) return false;
                if ((relationFilter & ToMask(entityInfoExt.Relation)) == 0) return false;
                if ((sourceFilter & ToMask(entityInfoExt.Source)) == 0) return false;
                return true;
            }
        }
    }
}
