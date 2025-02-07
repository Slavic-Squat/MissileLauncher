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
        public class GenericEntity : EntityInfo
        {
            public EntityType Type { get; set; }

            public enum EntityType : byte
            {
                Unknown, Ship, Station, Me
            }

            public GenericEntity(long entityID, Vector3 position, Vector3 velocity, DateTime timeRecorded, string name = "Unknown", string factionTag = "Unknown", EntityRelation relation = EntityRelation.Unknown, EntityType type = EntityType.Unknown)
                : base(entityID, position, velocity, timeRecorded, name, factionTag, relation)
            {
                Type = type;
            }

            public static GenericEntity CreateFromRaycast(MyDetectedEntityInfo entityInfo, DateTime timeRecorded)
            {
                return new GenericEntity(
                    entityID: entityInfo.EntityId,
                    position: entityInfo.Position,
                    velocity: entityInfo.Velocity,
                    timeRecorded: timeRecorded
                    );
            }
        }
    }
}
