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
            public static MySprite CreateText(Vector2 pos, string text, Color color, float scale = 1f, TextAlignment alignment = TextAlignment.LEFT, bool vertCentered = false)
            {
                if (vertCentered)
                {
                    float textHeight = MeasureStringInPixels(text, "White", scale).Y;
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

            public static MySprite CreateText(RectangleF bounds, string text, Color color, float scale = 1f, TextAlignment alignment = TextAlignment.LEFT, bool vertCentered = false, float padding = 0f)
            {
                bounds.Size -= 2 * padding;
                bounds.Position += padding;
                Vector2 pos = bounds.Position;

                Vector2 textSize = MeasureStringInPixels(text, "White", scale);
                float fillScale = Math.Min(bounds.Size.X / textSize.X, bounds.Size.Y / textSize.Y);

                if (vertCentered)
                {
                    pos.Y = bounds.Center.Y - (textSize.Y * fillScale) / 2;
                }

                switch (alignment)
                {
                    case TextAlignment.LEFT:
                        break;
                    case TextAlignment.RIGHT:
                        pos.X = bounds.Right;
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
                    RotationOrScale = fillScale,
                    Alignment = alignment,
                    FontId = "White"
                };
            }

            public static Vector2 MeasureStringInPixels(string text, string font = "White", float scale = 1f)
            {
                IMyTextSurface referenceSurface = MePb.GetSurface(0);
                var sb = new StringBuilder(text);
                return referenceSurface.MeasureStringInPixels(sb, font, scale);
            }
        }
    }
}
