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
        public class Button : IButton
        {
            public double Time { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => Bounds.Position;
            public Vector2 Size => Bounds.Size;
            public Vector2 Center => Bounds.Center;
            public bool CanPress => _canPress?.Invoke() ?? true;
            public bool IsHighlighted => _state.HasFlag(ButtonState.Highlighted);

            [Flags]
            public enum ButtonState
            {
                None = 0, Highlighted = 1, Pressed = 1 << 1, Disabled = 1 << 2
            }

            private RectangleF _bounds;

            private float _padding;
            private float _borderThickness;
            private float _highlightThickness;

            private Action _action;
            private ButtonState _state = ButtonState.None;
            private double _timePressed;
            private Func<bool> _canPress;
            private Func<string> _textGetter;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;
            private MySprite _textSprite;

            public Button(Vector2 pos, Vector2 size, float padding, float borderThickness, float highlightThickness, Func<string> textGetter, Action action, Func<bool> canPress = null)
            {
                _bounds = new RectangleF(pos, size);
                _padding = padding;
                _borderThickness = borderThickness;
                _highlightThickness = highlightThickness;

                _textGetter = textGetter;
                _action = action;
                _canPress = canPress;
            }

            private void BuildSprites()
            {
                Color fillColor, borderColor, textColor;
                RectangleF bounds = Bounds;
                if (_state.HasFlag(ButtonState.Disabled))
                {
                    fillColor = UIConfig.ButtonFillColorDisabled;
                    borderColor = UIConfig.ButtonBorderColorDisabled;
                    textColor = UIConfig.ButtonTextColorDisabled;
                }
                else if (_state.HasFlag(ButtonState.Pressed))
                {
                    fillColor = UIConfig.ButtonFillColorPressed;
                    borderColor = UIConfig.ButtonBorderColorPressed;
                    textColor = UIConfig.ButtonTextColorPressed;
                }
                else
                {
                    fillColor = IsHighlighted ? UIConfig.ButtonFillColorHighlighted : UIConfig.ButtonFillColor;
                    borderColor = UIConfig.ButtonBorderColor;
                    textColor = UIConfig.ButtonTextColor;
                }

                float scale = 1f;
                if (_state.HasFlag(ButtonState.Pressed))
                {
                    scale = 0.95f;
                }
                else if (_state.HasFlag(ButtonState.Highlighted))
                {
                    scale = 1.05f;
                }

                bounds.Size = Size * scale;
                bounds.Position = Pos + (Size - bounds.Size) / 2;

                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = bounds.Center,
                    Size = bounds.Size - 2 * _borderThickness * scale,
                    Color = fillColor,
                    Alignment = TextAlignment.CENTER
                };

                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = bounds.Center,
                    Size = bounds.Size,
                    Color = borderColor,
                    Alignment = TextAlignment.CENTER
                };

                _highlightSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = bounds.Center,
                    Size = bounds.Size + 2 * _highlightThickness * scale,
                    Color = UIConfig.ButtonHighlightColor,
                    Alignment = TextAlignment.CENTER
                };

                _textSprite = SpriteHelper.CreateText(bounds, _textGetter(), textColor, alignment: TextAlignment.CENTER, vertCentered: true, padding: _borderThickness + _padding);
            }

            public void Press()
            {
                if (!CanPress)
                {
                    return;
                }
                _action?.Invoke();
                _timePressed = Time;
                _state |= ButtonState.Pressed;
            }

            public void Highlight()
            {
                _state |= ButtonState.Highlighted;
            }

            public void Unhighlight()
            {
                _state &= ~ButtonState.Highlighted;
            }

            public void Update(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }

                if ((time - _timePressed) > 1f && _state.HasFlag(ButtonState.Pressed))
                {
                    _state &= ~ButtonState.Pressed;
                }

                if (CanPress && _state.HasFlag(ButtonState.Disabled))
                {
                    _state &= ~ButtonState.Disabled;
                }
                else if (!CanPress && !_state.HasFlag(ButtonState.Disabled))
                {
                    _state |= ButtonState.Disabled;
                }
                Time = time;
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                if (IsHighlighted)
                {
                    frame.Add(_highlightSprite);
                }
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);
                frame.Add(_textSprite);
            }
        }
    }
}
