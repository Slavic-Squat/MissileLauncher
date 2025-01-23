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
        public struct LauncherInfo
        {
            public string Name { get; }
            public long EntityID { get; }
            public Vector3 Position { get; }
            public Vector3 Velocity { get; }
            public DateTime TimeRecorded { get; }

            public LauncherInfo(string name, long entityID, Vector3 position, Vector3 velocity, DateTime timeRecorded)
            {
                Name = name;
                EntityID = entityID;
                Position = position;
                Velocity = velocity;
                TimeRecorded = timeRecorded;
            }

            public bool IsEmpty()
            {
                return EntityID == 0;
            }

            public static MyTuple<string, long, Vector3, Vector3, long> ToIGC(LauncherInfo launcherInfo)
            {
                return new MyTuple<string, long, Vector3, Vector3, long>(launcherInfo.Name, launcherInfo.EntityID, launcherInfo.Position, launcherInfo.Velocity, launcherInfo.TimeRecorded.Ticks);
            }

            public static LauncherInfo FromIGC(MyTuple<string, long, Vector3, Vector3, long> message)
            {
                return new LauncherInfo(message.Item1, message.Item2, message.Item3, message.Item4, new DateTime(message.Item5));
            }
        }
    }
}
