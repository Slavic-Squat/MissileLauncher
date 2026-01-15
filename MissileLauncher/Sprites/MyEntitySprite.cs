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
        public struct MyEntitySprite : IPositional2D
        {
            public Vector2 Pos => Sprite.Pos;
            public EntityInfoExt EntityInfo { get; private set; }
            public MySpriteExt Sprite { get; private set; }
            public bool IsValid { get; private set; }

            public MyEntitySprite(EntityInfoExt entityInfo, MySpriteExt sprite)
            {
                EntityInfo = entityInfo;
                Sprite = sprite;
                IsValid = true;
            }
        }
    }
}
