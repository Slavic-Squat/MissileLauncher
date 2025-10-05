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
        public class InfoPanel : IPanel, IUpdatable
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;

            public Func<string> TextGetter { get; set; }

            private RectangleF _bounds;

            private IMyTextSurface _surface;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _textSprite;

            public InfoPanel(Vector2 pos, Vector2 size, Func<string> textGetter, IMyTextSurface surface)
            {
                _bounds = new RectangleF(pos, size);
                TextGetter = textGetter;
                _surface = surface;

                BuildSprites();
            }

            public void BuildSprites()
            {
                float minDim = Math.Min(_bounds.Width, _bounds.Height);

                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 0.5f * 0.1f * minDim,
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
                _textSprite = SpriteHelper.CreateText(Bounds, TextGetter(), Color.White, _surface, TextAlignment.LEFT, false, 0.1f);
            }

            public void Update(DateTime time)
            {
                _textSprite = SpriteHelper.CreateText(Bounds, TextGetter(), Color.White, _surface, TextAlignment.LEFT, false, 0.1f);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);
                frame.Add(_textSprite);
            }
        }
    }
}
