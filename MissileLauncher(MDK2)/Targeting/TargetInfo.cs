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
        public struct TargetInfo : IEntityInfo
        {
            public long EntityID { get; private set; }
            public Vector3 Position { get; set;  }
            public Vector3 Velocity { get; set; }
            public DateTime TimeRecorded { get; set; }

            public TargetInfo(long entityID, Vector3 position, Vector3 velocity, DateTime timeRecorded)
            {
                EntityID = entityID;
                Position = position;
                Velocity = velocity;
                TimeRecorded = timeRecorded;
            }

            public TargetInfo(MyDetectedEntityInfo entityInfo, DateTime timeRecorded)
            {
                EntityID = entityInfo.EntityId;
                Position = entityInfo.Position;
                Velocity = entityInfo.Velocity;
                TimeRecorded = timeRecorded;
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
                Position = entityInfo.Position;
                Velocity = entityInfo.Velocity;
                TimeRecorded = timeRecorded;
            }

            public void Merge(IEntityInfo entityInfo)
            {
                if (EntityID != entityInfo.EntityID || TimeRecorded > entityInfo.TimeRecorded)
                {
                    return;
                }
                Position = entityInfo.Position;
                Velocity = entityInfo.Velocity;
                TimeRecorded = entityInfo.TimeRecorded;
            }

            public byte[] Serialize()
            {
                List<byte> entityData = new List<byte>
                {
                    (byte)ObjectTypes.TargetInfo
                };

                entityData.AddRange(BitConverter.GetBytes(EntityID));

                entityData.AddRange(BitConverter.GetBytes(Position.X));
                entityData.AddRange(BitConverter.GetBytes(Position.Y));
                entityData.AddRange(BitConverter.GetBytes(Position.Z));

                entityData.AddRange(BitConverter.GetBytes(Velocity.X));
                entityData.AddRange(BitConverter.GetBytes(Velocity.Y));
                entityData.AddRange(BitConverter.GetBytes(Velocity.Z));

                entityData.AddRange(BitConverter.GetBytes(TimeRecorded.Ticks));

                return entityData.ToArray();
            }

            public static TargetInfo Deserialize(byte[] data)
            {
                int index = 1;

                long entityID = BitConverter.ToInt64(data, index);
                index += 8;

                float xPos = BitConverter.ToSingle(data, index);
                index += 4;

                float yPos = BitConverter.ToSingle(data, index);
                index += 4;

                float zPos = BitConverter.ToSingle(data, index);
                index += 4;

                Vector3 pos = new Vector3(xPos, yPos, zPos);

                float xVel = BitConverter.ToSingle(data, index);
                index += 4;

                float yVel = BitConverter.ToSingle(data, index);
                index += 4;

                float zVel = BitConverter.ToSingle(data, index);
                index += 4;

                Vector3 vel = new Vector3(xVel, yVel, zVel);

                long ticks = BitConverter.ToInt64(data, index);
                index += 8;

                DateTime timeRecorded = new DateTime(ticks);

                return new TargetInfo(entityID, pos, vel, timeRecorded);
            }
        }
    }
}
