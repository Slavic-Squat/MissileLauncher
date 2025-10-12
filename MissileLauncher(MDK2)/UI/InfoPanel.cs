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
        public class InfoPanel : IPanel
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;

            private Func<string> _textGetter;

            private RectangleF _bounds;
            private float _borderThickness;
            private float _padding;

            private IMyTextSurface _surface;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _textSprite;

            public InfoPanel(Vector2 pos, Vector2 size, float borderThickness, float padding, Func<string> textGetter, IMyTextSurface surface)
            {
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _textGetter = textGetter;
                _surface = surface;
                _padding = padding;
            }

            private void BuildSprites()
            {
                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 2 * _borderThickness,
                    RotationOrScale = 0f,
                    Color = UIConfig.PanelFillColor,
                    Alignment = TextAlignment.CENTER,
                };
                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    RotationOrScale = 0f,
                    Color = UIConfig.PanelBorderColor,
                    Alignment = TextAlignment.CENTER,
                };
                _textSprite = SpriteHelper.CreateText(Bounds, _textGetter(), Color.White, _surface, TextAlignment.LEFT, false, _padding);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);
                frame.Add(_textSprite);
            }
        }
    }
}
