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
        public class MissileInfoLite : EntityInfo
        {
            public long LauncherID { get; private set; }

            public MissileInfoLite(long entityID, Vector3 position, Vector3 velocity, DateTime timeRecorded, long launcherID)
                : base(entityID, position, velocity, timeRecorded)
            {
                LauncherID = launcherID;
            }

            public override byte[] Serialize()
            {
                byte[] baseData = base.Serialize();

                List<byte> missileData = new List<byte>();
                baseData[0] = (byte)Deserializer.ObjectTypes.MissileInfoLite;
                missileData.AddRange(baseData);
                missileData.AddRange(BitConverter.GetBytes(LauncherID));

                return missileData.ToArray();
            }

            public static new MissileInfoLite Deserialize(byte[] data)
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

                return new MissileInfoLite(entityID, pos, vel, timeRecorded, launcherID);
            }
        }
    }
}
