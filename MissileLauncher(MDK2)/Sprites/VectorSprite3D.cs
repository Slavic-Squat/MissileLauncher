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
        public class VectorSprite3D : Sprite3D
        {
            public VectorSpriteDefinition Definition {  get; set; }
            public Vector3 Position { get; set; }
            float Z {  get; set; }
            public Vector3 Origin { get; set; }
            public Vector3 EndPoint { get; set; }
            public Vector3 MidPoint { get; set; }
            public float Length { get; set; }
            public Color SpriteColor { get; set; }
            public float Rotation { get; set; }

            public VectorSprite3D(VectorSpriteDefinition definition, Vector3 origin, Vector3 endPoint, Color? color)
            {
                Origin = origin;
                EndPoint = endPoint;
                MidPoint = (endPoint + origin) / 2;
                var diff = endPoint - origin;
                Length = diff.Length();
                Rotation = (float)-Math.Atan2(diff.Y, diff.X);

                Position = MidPoint;
                Z = origin.Z;

                SpriteColor = color ?? definition.SpriteColor;

            }

            public override MySprite ToMySprite(Vector2 screenSize)
            {
                Vector2 posScreen = new Vector2(MidPoint.X, -MidPoint.Y) * screenSize.X / 2 + screenSize / 2;
                Vector2 sizeScreen = new Vector2(Length / 2 * screenSize.X, 1);
                return new MySprite(type: SpriteType.TEXTURE, data: Definition.Name, position: posScreen, size: sizeScreen, color: SpriteColor, rotation: Rotation);
            }
        }
    }
}
