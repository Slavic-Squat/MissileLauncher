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
        public class SpriteDefinition
        {
            public string Name { get; set; }
            public float NativeAR {  get; set; }
            public float BaseScale { get; set; }
            public float MinDepthScale { get; set; }
            public float MaxDepthScale { get; set; }
            public Color SpriteColor { get; set; }
            public float Rotation { get; set; }

        }
    }
}
