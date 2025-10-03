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
        public class InfoModal : IModal
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => Bounds.Position;
            public Vector2 Size => Bounds.Size;
            public Vector2 Center => Bounds.Center;
            public bool CanClose => _condition.Invoke();

            private RectangleF _bounds;
            private string _text;
            private Func<bool> _condition;
            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _textSprite;

            private IMyTextSurface _surface;

            public InfoModal(Vector2 pos, Vector2 size, Func<bool> condition, string text, IMyTextSurface surface)
            {
                _bounds = new RectangleF(pos, size);
                _text = text;
                _condition = condition;
                _surface = surface;

                BuildSprites();
            }

            public void BuildSprites()
            {
                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 20f,
                    Color = new Color(0, 0, 0, 200),
                    Alignment = TextAlignment.CENTER
                };

                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER
                };

                _textSprite = SpriteHelper.CreateText(Bounds, _text, Color.White, _surface, TextAlignment.CENTER, true, 0.2f);
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
