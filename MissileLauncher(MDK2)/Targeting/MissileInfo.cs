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
        public struct MissileInfo : IEntityInfo
        {
            public long EntityID { get; private set; }
            public Vector3 Position { get; set; }
            public Vector3 Velocity { get; set; }
            public DateTime TimeRecorded { get; set; }
            public long LauncherID { get; private set; }
            public MissileStage Stage { get; set; }
            public long TargetID { get; set; }
            public long Address { get; set; }

            public MissileInfo(long entityID, Vector3 position, Vector3 velocity, DateTime timeRecorded, long launcherID, long targetID, MissileStage stage, long address)
            {
                EntityID = entityID;
                Position = position;
                Velocity = velocity;
                TimeRecorded = timeRecorded;
                LauncherID = launcherID;
                TargetID = targetID;
                Stage = stage;
                Address = address;
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
                if (entityInfo is MissileInfo)
                {
                    var other = (MissileInfo)entityInfo;
                    if (other.LauncherID != LauncherID)
                    {
                        return;
                    }
                    TargetID = other.TargetID;
                    Stage = other.Stage;
                }
                Position = entityInfo.Position;
                Velocity = entityInfo.Velocity;
                TimeRecorded = entityInfo.TimeRecorded;
            }

            public byte[] Serialize()
            {
                List<byte> missileData = new List<byte>
                {
                    (byte)ObjectTypes.MissileInfo
                };
                missileData.AddRange(BitConverter.GetBytes(EntityID));

                missileData.AddRange(BitConverter.GetBytes(Position.X));
                missileData.AddRange(BitConverter.GetBytes(Position.Y));
                missileData.AddRange(BitConverter.GetBytes(Position.Z));

                missileData.AddRange(BitConverter.GetBytes(Velocity.X));
                missileData.AddRange(BitConverter.GetBytes(Velocity.Y));
                missileData.AddRange(BitConverter.GetBytes(Velocity.Z));

                missileData.AddRange(BitConverter.GetBytes(TimeRecorded.Ticks));

                missileData.AddRange(BitConverter.GetBytes(LauncherID));
                missileData.AddRange(BitConverter.GetBytes(TargetID));
                missileData.Add((byte)Stage);
                missileData.AddRange(BitConverter.GetBytes(Address));

                return missileData.ToArray();
            }

            public static MissileInfo Deserialize(byte[] data)
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

                long launcherID = BitConverter.ToInt64(data, index);
                index += 8;

                long targetID = BitConverter.ToInt64(data, index);
                index += 8;

                MissileStage stage = (MissileStage)data[index];
                index += 1;

                long address = BitConverter.ToInt64(data, index);
                index += 8;

                return new MissileInfo(entityID, pos, vel, timeRecorded, launcherID, targetID, stage, address);
            }
        }
    }
}
