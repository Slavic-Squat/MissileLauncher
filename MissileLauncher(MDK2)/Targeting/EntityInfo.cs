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
        public abstract class EntityInfo
        {
            public string Name { get; set; }
            public string FactionTag { get; set; }
            public long EntityID { get; set; }
            public Vector3 Position { get; set;  }
            public Vector3 Velocity { get; set; }
            public DateTime TimeRecorded { get; set; }
            public EntityRelation Relation {  get; set; }

            public enum EntityRelation : byte
            {
                Unknown, Neutral, Friendly, Hostile
            }

            public EntityInfo(long entityID, Vector3 position, Vector3 velocity, DateTime timeRecorded, string name = "Unknown", string factionTag = "Unknown", EntityRelation relation = EntityRelation.Unknown)
            {
                Name = name;
                EntityID = entityID;
                Position = position;
                Velocity = velocity;
                TimeRecorded = timeRecorded;
                Relation = relation;
                
            }

            public void Transform(Matrix transform)
            {
                var postion = Vector3.Transform(Position, transform);
                var velocity = Vector3.Transform(Velocity, transform);

                Position = postion;
                Velocity = velocity;
            }

            public void UpdateFromRaycast(MyDetectedEntityInfo entityInfo, DateTime timeRecorded)
            {
                EntityID = entityInfo.EntityId;
                Position = entityInfo.Position;
                Velocity = entityInfo.Velocity;
                TimeRecorded = timeRecorded;
            }

            public abstract string Serialize();

            public static EntityInfo Deserialize(byte[] data)
            {

            }
        }
    }
}
