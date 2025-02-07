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
        public class GenericSprite3D : Sprite3D
        {
            public SpriteDefinition Definition { get; private set; }
            public float Scale { get; private set; }

            public GenericSprite3D(SpriteDefinition definition, Vector3 position, float scale, float? rotation = null, Color? color = null)
            {
                Definition = definition;
                Position = position;
                Z = Position.Z;
                Rotation = rotation ?? definition.Rotation;
                SpriteColor = color ?? definition.SpriteColor;
                Scale = scale * definition.BaseScale * (definition.MinDepthScale + (definition.MaxDepthScale - definition.MinDepthScale) * (1 - Z));
            }

            public override MySprite ToMySprite(Vector2 screenSize)
            {
                Vector2 posScreen = new Vector2(Position.X, -Position.Y) * screenSize.X / 2 + screenSize / 2;
                Vector2 sizeScreen = new Vector2(1, 1 / Definition.NativeAR) * Scale * screenSize.X;
                return new MySprite(type: SpriteType.TEXTURE, data: Definition.Name, position: posScreen, size: sizeScreen, color: SpriteColor, rotation: Rotation);
            }
        }
    }
}
