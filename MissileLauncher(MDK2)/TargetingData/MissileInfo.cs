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
        public class MissileInfo : EntityInfo
        {
            public MissileStage Stage { get; set; }
            public long TargetID { get; set; }
            public long LauncherID { get; set; }

            public enum MissileStage : byte
            {
                Unknown, Flying, Interception
            }

            public MissileInfo(long entityID, Vector3 position, Vector3 velocity, DateTime timeRecorded, string name = "Unknown", string factionTag = "Unknown", EntityRelation relation = EntityRelation.Unknown, long launcherID = -1, long targetID = -1, MissileStage stage = MissileStage.Unknown)
                : base(entityID, position, velocity, timeRecorded, name, factionTag, relation)
            {
                LauncherID = launcherID;
                TargetID = targetID;
                Stage = stage;
            }
        }
    }
}
