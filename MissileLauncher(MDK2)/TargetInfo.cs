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
        public struct TargetInfo
        {
            public string Name { get;}
            public long EntityID { get;}
            public Vector3 Position { get;}
            public Vector3 Velocity { get;}
            public DateTime TimeRecorded { get;}

            public TargetInfo(long entityID, Vector3 position, Vector3 velocity, DateTime timeRecorded, string name = "Default")
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
        }
    }
}
