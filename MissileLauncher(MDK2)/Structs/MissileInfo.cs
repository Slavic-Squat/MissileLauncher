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
        public struct MissileInfo
        {
            public string Name { get; }
            public long EntityID { get; }
            public string Stage { get; }
            public long TargetID { get; }
            public Vector3 Position { get; }
            public Vector3 Velocity { get; }
            public DateTime TimeRecorded { get; }

            public MissileInfo(long entityID, long targetID, Vector3 position, Vector3 velocity, DateTime timeRecorded, string name = "Unknown", string stage = "Unknown")
            {
                Name = name;
                EntityID = entityID;
                Stage = stage;
                TargetID = targetID;
                Position = position;
                Velocity = velocity;
                TimeRecorded = timeRecorded;
            }

            public bool IsEmpty()
            {
                return EntityID == 0;
            }

            public static MyTuple<MyTuple<long, long, Vector3, Vector3, long>, MyTuple<string, string>> ToIGC(MissileInfo missileInfo)
            {
                var part0 = new MyTuple<long, long, Vector3, Vector3, long>(missileInfo.EntityID, missileInfo.TargetID, missileInfo.Position, missileInfo.Velocity, missileInfo.TimeRecorded.Ticks);
                var part1 = new MyTuple<string, string>(missileInfo.Name, missileInfo.Stage);
                return new MyTuple<MyTuple<long, long, Vector3, Vector3, long>, MyTuple<string, string>>(part0, part1);
            }

            public static MissileInfo FromIGC(MyTuple<MyTuple<long, long, Vector3, Vector3, long>, MyTuple<string, string>> message)
            {
                var part0 = message.Item1;
                var part1 = message.Item2;
                return new MissileInfo(part0.Item1, part0.Item2, part0.Item3, part0.Item4, new DateTime(part0.Item5), name: part1.Item1, stage: part1.Item2);
            }
        }
    }
}
