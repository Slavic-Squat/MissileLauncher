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
        public static class Deserializer
        {
            public enum ObjectTypes : byte
            {
                Command, EntityInfo, MissileInfo
            }
            public static object Deserialize(string dataString)
            {
                byte[] data = Convert.FromBase64String(dataString);

                switch ((ObjectTypes)data[0])
                {
                    case ObjectTypes.Command:
                        return Encoding.ASCII.GetString(data, 1, data.Length - 1);

                    case ObjectTypes.EntityInfo:
                        return EntityInfo.Deserialize(data);

                    case ObjectTypes.MissileInfo:
                        return MissileInfo.Deserialize(data);
                }

                return null;
            }
        }
    }
}
