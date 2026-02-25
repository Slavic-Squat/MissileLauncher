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

            private double _lastUpdateTime;
            private RectangleF _bounds;

            private float _padding;
            private float _borderThickness;
            private float _highlightThickness;

            private Action _action;
            private ButtonState _state = ButtonState.None;
            private double _timePressed;
            private Func<bool> _canPress;
            private Func<string> _textGetter;

            private List<MySprite> _bodySprites = new List<MySprite>();
            private MySprite _highlightSprite;
            private MySprite _textSprite;

            private StringBuilder _sb = new StringBuilder();
            private IMyTextSurface _surface;

            public Button(IMyTextSurface surface, Vector2 pos, Vector2 size, float padding, float borderThickness, float highlightThickness, Func<string> textGetter, Action action, Func<bool> canPress = null)
            {
                _surface = surface;
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

                _bodySprites.Clear();
                SpriteHelper.CreateBoxFilled(_bodySprites, bounds, borderColor, fillColor, _borderThickness * scale);

                _highlightSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = bounds.Center,
                    Size = bounds.Size + 2 * _highlightThickness * scale,
                    Color = UIConfig.ButtonHighlightColor,
                    Alignment = TextAlignment.CENTER
                };

                string text = _textGetter();
                _sb.Clear();
                _sb.Append(text);
                _textSprite = SpriteHelper.CreateText(bounds.Center, _sb, textColor, _surface, text: text, alignment: TextAlignment.CENTER, vertCentered: true, maxHeight: bounds.Height - 2f * (_borderThickness + _padding), maxWidth: bounds.Width - 2f * (_borderThickness + _padding), fontID: "Monospace");
            }

            public void Press()
            {
                if (!CanPress)
                {
                    return;
                }
                _action?.Invoke();
                _timePressed = SystemTime;
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
                if (_lastUpdateTime == 0)
                {
                    _lastUpdateTime = time;
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
                _lastUpdateTime = time;
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                if (IsHighlighted)
                {
                    frame.Add(_highlightSprite);
                }
                frame.AddRange(_bodySprites);
                frame.Add(_textSprite);
            }
        }
    }
}
