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
        public static class SpriteHelper
        {
            //public static List<MySprite> CreateBorder(Vector2 pos, Vector2 size, Color color, float borderThickness)
            //{
            //    List<MySprite> sprites = new List<MySprite>();
            //    MySprite topBorder = new MySprite()
            //    {
            //        Type = SpriteType.TEXTURE,
            //        Data = "SquareSimple",
            //        Position = pos + new Vector2(0, -size.Y / 2 + borderThickness / 2),
            //        Size = new Vector2(size.X, borderThickness),
            //        Color = color,
            //        Alignment = TextAlignment.CENTER
            //    };
            //    sprites.Add(topBorder);
            //    MySprite bottomBorder = new MySprite()
            //    {
            //        Type = SpriteType.TEXTURE,
            //        Data = "SquareSimple",
            //        Position = pos + new Vector2(0, size.Y / 2 - borderThickness / 2),
            //        Size = new Vector2(size.X, borderThickness),
            //        Color = color,
            //        Alignment = TextAlignment.CENTER
            //    };
            //    sprites.Add(bottomBorder);
            //    MySprite leftBorder = new MySprite()
            //    {
            //        Type = SpriteType.TEXTURE,
            //        Data = "SquareSimple",
            //        Position = pos + new Vector2(-size.X / 2 + borderThickness / 2, 0),
            //        Size = new Vector2(borderThickness, size.Y),
            //        Color = color,
            //        Alignment = TextAlignment.CENTER
            //    };
            //    sprites.Add(leftBorder);
            //    MySprite rightBorder = new MySprite()
            //    {
            //        Type = SpriteType.TEXTURE,
            //        Data = "SquareSimple",
            //        Position = pos + new Vector2(size.X / 2 - borderThickness / 2, 0),
            //        Size = new Vector2(borderThickness, size.Y),
            //        Color = color,
            //        Alignment = TextAlignment.CENTER
            //    };
            //    sprites.Add(rightBorder);
            //    return sprites;
            //}

            public static MySprite CreateText(Vector2 pos, string text, Color color, IMyTextSurface surface, float scale = 1f, TextAlignment alignment = TextAlignment.LEFT, bool vertCentered = false)
            {
                var sb = new StringBuilder(text);

                if (vertCentered)
                {
                    float textHeight = surface.MeasureStringInPixels(sb, "White", scale).Y;
                    pos.Y -= textHeight / 2;
                }
                return new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Position = pos,
                    Color = color,
                    RotationOrScale = scale,
                    Alignment = alignment,
                    FontId = "White"
                };
            }

            public static MySprite CreateText(RectangleF bounds, string text, Color color, IMyTextSurface surface, TextAlignment alignment = TextAlignment.LEFT, bool vertCentered = false, float scale = 1f, float padding = 0f)
            {
                var sb = new StringBuilder(text);

                Vector2 textSize = surface.MeasureStringInPixels(sb, "White", 1f);
                float fillScale = Math.Min(bounds.Size.X / textSize.X, bounds.Size.Y / textSize.Y);

                Vector2 pos = bounds.Position;

                if (vertCentered)
                {
                    pos.Y = bounds.Center.Y - (textSize.Y * fillScale * scale) / 2;
                }
                else
                {
                    pos.Y += padding;
                }

                switch (alignment)
                {
                    case TextAlignment.LEFT:
                        pos.X += padding;
                        break;
                    case TextAlignment.RIGHT:
                        pos.X = bounds.Right - padding;
                        break;
                    case TextAlignment.CENTER:
                        pos.X = bounds.Center.X;
                        break;
                }

                return new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Position = pos,
                    Color = color,
                    RotationOrScale = fillScale * scale,
                    Alignment = alignment,
                    FontId = "White"
                };
            }
        }
    }
}
