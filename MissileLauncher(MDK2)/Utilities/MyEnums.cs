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
        public enum EntitySource : byte
        {
            None = 0, Local = 1, Remote = 1 << 1, Both = Local | Remote
        }

        public static string GetDisplayString(EntitySource source)
        {
            switch (source)
            {
                case EntitySource.None: return "NONE";
                case EntitySource.Local: return "LOCAL";
                case EntitySource.Remote: return "REMOTE";
                case EntitySource.Both: return "BOTH";
                default: return "N/A";
            }
        }

        public enum BayStatus : byte
        {
            Empty, Loaded, Ready, Active, Launching
        }

        public static string GetDisplayString(BayStatus status)
        {
            switch (status)
            {
                case BayStatus.Empty: return "EMPTY";
                case BayStatus.Loaded: return "LOADED";
                case BayStatus.Ready: return "READY";
                case BayStatus.Active: return "ACTIVE";
                case BayStatus.Launching: return "LAUNCHING";
                default: return "N/A";
            }
        }

        public enum MissileType : byte
        {
            Unknown, AntiShip, AntiMissile, Cluster
        }

        public static MissileType GetMissileType(string typeStr)
        {
            switch (typeStr.ToUpper())
            {
                case "ANTI-SHIP": return MissileType.AntiShip;
                case "ANTI-MISL": return MissileType.AntiMissile;
                case "CLUSTER": return MissileType.Cluster;
                default: return MissileType.Unknown;
            }
        }

        public static string GetDisplayString(MissileType type)
        {
            switch (type)
            {
                case MissileType.Unknown: return "N/A";
                case MissileType.AntiShip: return "ANTI-SHIP";
                case MissileType.AntiMissile: return "ANTI-MISL";
                case MissileType.Cluster: return "CLUSTER";
                default: return "N/A";
            }
        }

        public enum MissileGuidanceType : byte
        {
            Unknown, MCLOS,
        }

        public static MissileGuidanceType GetMissileGuidanceType(string typeStr)
        {
            switch (typeStr.ToUpper())
            {
                case "MCLOS": return MissileGuidanceType.MCLOS;
                default: return MissileGuidanceType.Unknown;
            }
        }

        public static string GetDisplayString(MissileGuidanceType type)
        {
            switch (type)
            {
                case MissileGuidanceType.Unknown: return "N/A";
                case MissileGuidanceType.MCLOS: return "MCLOS";
                default: return "N/A";
            }
        }

        public enum MissilePayload : byte
        {
            Unknown, HE, Nuclear, Kinectic
        }

        public static MissilePayload GetMissilePayload(string payloadStr)
        {
            switch (payloadStr.ToUpper())
            {
                case "HE": return MissilePayload.HE;
                case "NUKE": return MissilePayload.Nuclear;
                case "KINECTIC": return MissilePayload.Kinectic;
                default: return MissilePayload.Unknown;
            }
        }

        public static string GetDisplayString(MissilePayload payload)
        {
            switch (payload)
            {
                case MissilePayload.Unknown: return "N/A";
                case MissilePayload.HE: return "HE";
                case MissilePayload.Nuclear: return "NUKE";
                case MissilePayload.Kinectic: return "KINECTIC";
                default: return "N/A";
            }
        }

        public enum MissileStage : byte
        {
            Unknown, Idle, Active, Launching, Flying, Interception
        }

        public static string GetDisplayString(MissileStage stage)
        {
            switch (stage)
            {
                case MissileStage.Unknown: return "N/A";
                case MissileStage.Idle: return "IDLE";
                case MissileStage.Launching: return "LAUNCHING";
                case MissileStage.Flying: return "FLYING";
                case MissileStage.Interception: return "INTERCEPTION";
                default: return "N/A";
            }
        }

        public enum EntityType : byte
        {
            Target, Missile
        }

        public static string GetDisplayString(EntityType type)
        {
            switch (type)
            {
                case EntityType.Target: return "TRGT";
                case EntityType.Missile: return "MISL";
                default: return "N/A";
            }
        }

        public enum EntityInfoSubType : byte
        {
            None, MissileInfoLite, MissileInfo,
        }

        public enum EntityRelation : byte
        {
            Neutral, Hostile, Friendly, Me
        }

        public static string GetDisplayString(EntityRelation relation)
        {
            switch (relation)
            {
                case EntityRelation.Neutral: return "NTRL";
                case EntityRelation.Hostile: return "HSTL";
                case EntityRelation.Friendly: return "FRND";
                case EntityRelation.Me: return "ME";
                default: return "N/A";
            }
        }

        public enum NavMode : byte
        {
            UI, Targeting,
        }

        public static readonly NavMode[] NavModeCycles = new NavMode[] { NavMode.UI, NavMode.Targeting };

        public static NavMode NextNavMode(NavMode mode)
        {
            int index = Array.IndexOf(NavModeCycles, mode);
            if (index < 0) return NavModeCycles[0];
            index = (index + 1) % NavModeCycles.Length;
            return NavModeCycles[index];
        }

        public static NavMode PreviousNavMode(NavMode mode)
        {
            int index = Array.IndexOf(NavModeCycles, mode);
            if (index < 0) return NavModeCycles[0];
            index = (index - 1 + NavModeCycles.Length) % NavModeCycles.Length;
            return NavModeCycles[index];
        }

        public static string GetDisplayString(NavMode mode)
        {
            switch (mode)
            {
                case NavMode.UI: return "UI";
                case NavMode.Targeting: return "TARGETING";
                default: return "N/A";
            }
        }

        [Flags]
        public enum EntityTypeFilter : byte
        {
            None = 0, Targets = 1 << 0, Missiles = 1 << 1, All = Targets | Missiles,
        }

        public static readonly EntityTypeFilter[] EntityTypeFilterCycles = new EntityTypeFilter[] { EntityTypeFilter.Targets, EntityTypeFilter.Missiles, EntityTypeFilter.All };

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

        [Flags]
        public enum EntityRelationFilter : byte
        {
            None = 0, Hostile = 1 << 0, Neutral = 1 << 1, Friendly = 1 << 2, Me = 1 << 3, All = Hostile | Neutral | Friendly | Me,
        }

        public static readonly EntityRelationFilter[] EntityRelationFilterCycles = new EntityRelationFilter[] { EntityRelationFilter.Hostile, EntityRelationFilter.Neutral, EntityRelationFilter.Friendly, EntityRelationFilter.Me, EntityRelationFilter.All };

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

        [Flags]
        public enum EntitySourceFilter : byte
        {
            None = 0, Local = 1 << 0, Remote = 1 << 1, Both = Local | Remote
        }

        public static readonly EntitySourceFilter[] EntitySourceFilterCycles = new EntitySourceFilter[] { EntitySourceFilter.Local, EntitySourceFilter.Remote, EntitySourceFilter.Both };

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

        public enum ScopeScale : byte
        {
            Close, Medium, Far
        }

        public static readonly ScopeScale[] ScopeScaleCycles = new ScopeScale[] { ScopeScale.Close, ScopeScale.Medium, ScopeScale.Far };

        public static ScopeScale NextScopeScale(ScopeScale scale)
        {
            int index = Array.IndexOf(ScopeScaleCycles, scale);
            if (index < 0) return ScopeScaleCycles[0];
            index = (index + 1) % ScopeScaleCycles.Length;
            return ScopeScaleCycles[index];
        }

        public static ScopeScale PreviousScopeScale(ScopeScale scale)
        {
            int index = Array.IndexOf(ScopeScaleCycles, scale);
            if (index < 0) return ScopeScaleCycles[0];
            index = (index - 1 + ScopeScaleCycles.Length) % ScopeScaleCycles.Length;
            return ScopeScaleCycles[index];
        }

        public static string GetDisplayString(ScopeScale scale)
        {
            switch (scale)
            {
                case ScopeScale.Close: return "3km";
                case ScopeScale.Medium: return "6km";
                case ScopeScale.Far: return "12km";
                default: return "N/A";
            }
        }

        public static int GetValue(ScopeScale scale)
        {
            switch (scale)
            {
                case ScopeScale.Close: return 4;
                case ScopeScale.Medium: return 2;
                case ScopeScale.Far: return 1;
                default: return 0;
            }
        }

        public enum Direction
        {
            Left, Right, Up, Down, Forward, Backward
        }

        public static EntityTypeFilter ToMask(EntityType type)
        {
            switch (type)
            {
                case EntityType.Target: return EntityTypeFilter.Targets;
                case EntityType.Missile: return EntityTypeFilter.Missiles;
                default: return EntityTypeFilter.None;
            }
        }

        public static EntityRelationFilter ToMask(EntityRelation relation)
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

        public static EntitySourceFilter ToMask(EntitySource source)
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
