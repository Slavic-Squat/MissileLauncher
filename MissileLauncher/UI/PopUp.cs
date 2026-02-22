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
        public class PopUp : IPopUp
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => Bounds.Position;
            public Vector2 Size => Bounds.Size;
            public Vector2 Center => Bounds.Center;
            public bool CanClose => _condition.Invoke();

            private RectangleF _bounds;
            private float _borderThickness;
            private float _padding;
            private string _text;
            private Func<bool> _condition;
            private List<MySprite> _bodySprites = new List<MySprite>();
            private MySprite _textSprite;
            private MySprite _obscureSprite;
            private RectangleF _screenBounds;

            private StringBuilder _sb = new StringBuilder();
            private IMyTextSurface _surface;

            public PopUp(IMyTextSurface surface, Vector2 pos, Vector2 size, float borderThickness, float padding, Func<bool> condition, string text, RectangleF screenBounds)
            {
                _surface = surface;
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _text = text;
                _condition = condition;
                _padding = padding;
                _screenBounds = screenBounds;

                _sb.Append(text);
            }

            private void BuildSprites()
            {
                _bodySprites.Clear();
                SpriteHelper.CreateBoxFilled(_bodySprites, _bounds, new Color(252, 3, 94, 255), new Color(38, 19, 26, 255), _borderThickness);

                _obscureSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
                    Color = new Color(0, 0, 0, 229),
                    Alignment = TextAlignment.CENTER
                };

                _textSprite = SpriteHelper.CreateText(_bounds.Center, _sb, new Color(252, 3, 94, 255), _surface, text: _text, alignment: TextAlignment.CENTER, vertCentered: true, maxHeight: _bounds.Height - 2f * (_borderThickness + _padding), maxWidth: _bounds.Width - 2f * (_borderThickness + _padding), fontID: "Monospace");
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                frame.Add(_obscureSprite);
                frame.AddRange(_bodySprites);
                frame.Add(_textSprite);
            }
        }
    }
}
