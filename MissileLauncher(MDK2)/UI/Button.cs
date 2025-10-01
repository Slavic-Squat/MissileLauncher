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
            public string Name { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => Bounds.Position;
            public Vector2 Size => Bounds.Size;
            public Vector2 Center => Bounds.Center;
            public bool CanPress => _canPress?.Invoke() ?? true;
            public bool IsHighlighted => _state.HasFlag(ButtonState.Highlighted);

            [Flags]
            public enum ButtonState
            {
                None = 0, Highlighted = 1, Pressed = 1 << 1, Disabled = 1 << 2, Errored = 1 << 3
            }

            private RectangleF _bounds;
            private readonly Vector2 _originalPos;
            private readonly Vector2 _originalSize;

            private Func<bool> _action;
            private ButtonState _state = ButtonState.None;
            private DateTime _timePressed = DateTime.MinValue;
            private Func<bool> _canPress;
            private string _text;

            private Color _fillColor = UIConfig.ButtonFillColor;
            private Color _borderColor = UIConfig.ButtonBorderColor;
            private Color _textColor = UIConfig.ButtonTextColor;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;
            private MySprite _textSprite;

            private IMyTextSurface _surface;

            public Button(string name, Vector2 pos, Vector2 size, string text, Func<bool> action, IMyTextSurface surface, Func<bool> canPress = null)
            {
                Name = name;

                _bounds = new RectangleF(pos, size);
                _originalPos = pos;
                _originalSize = size;

                _text = text;
                _action = action;
                _surface = surface;
                _canPress = canPress;

                BuildSprites();
            }

            private void BuildSprites()
            {
                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 20,
                    Color = _fillColor,
                    Alignment = TextAlignment.CENTER
                };

                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    Color = _borderColor,
                    Alignment = TextAlignment.CENTER
                };

                _highlightSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size + 10f,
                    Color = UIConfig.ButtonHighlightColor,
                    Alignment = TextAlignment.CENTER
                };

                _textSprite = SpriteHelper.CreateText(Bounds, _text, _textColor, _surface, TextAlignment.CENTER, true, 0.75f);
            }

            public void Press(DateTime time)
            {
                if (!CanPress)
                {
                    return;
                }

                _timePressed = time;

                if (_action?.Invoke() == false)
                {
                    _state |= ButtonState.Errored;
                    return;
                }
                _state |= ButtonState.Pressed;
            }

            public void Highlight()
            {
                if (IsHighlighted)
                    return;

                _state |= ButtonState.Highlighted;
                _bounds.Position -= _originalSize * 0.05f;
                _bounds.Size = _originalSize * 1.1f;
            }

            public void Unhighlight()
            {
                if (!IsHighlighted)
                    return;

                _state &= ~ButtonState.Highlighted;
                _bounds.Position = _originalPos;
                _bounds.Size = _originalSize;
            }

            public void Update(DateTime time)
            {
                if (time - _timePressed > TimeSpan.FromSeconds(1) && _state.HasFlag(ButtonState.Pressed))
                {
                    _state &= ~ButtonState.Pressed;
                }
                else if (time - _timePressed > TimeSpan.FromSeconds(2) && _state.HasFlag(ButtonState.Errored))
                {
                    _state &= ~ButtonState.Errored;
                }

                if (CanPress && _state.HasFlag(ButtonState.Disabled))
                {
                    _state &= ~ButtonState.Disabled;
                }
                else if (!CanPress && !_state.HasFlag(ButtonState.Disabled))
                {
                    _state |= ButtonState.Disabled;
                }

                if (_state.HasFlag(ButtonState.Disabled))
                {
                    _fillColor = UIConfig.ButtonFillColorDisabled;
                    _borderColor = UIConfig.ButtonBorderColorDisabled;
                    _textColor = UIConfig.ButtonTextColorDisabled;
                }
                else if (_state.HasFlag(ButtonState.Errored))
                {
                    _fillColor = UIConfig.ButtonFillErrored;
                    _borderColor = UIConfig.ButtonBorderColorErrored;
                    _textColor = UIConfig.ButtonTextColorErrored;
                }
                else if (_state.HasFlag(ButtonState.Pressed))
                {
                    _fillColor = UIConfig.ButtonFillColorPressed;
                    _borderColor = UIConfig.ButtonBorderColorPressed;
                    _textColor = UIConfig.ButtonTextColorPressed;
                }
                else
                {
                    _fillColor = UIConfig.ButtonFillColor;
                    _borderColor = UIConfig.ButtonBorderColor;
                    _textColor = UIConfig.ButtonTextColor;
                }

                BuildSprites();
            }

            public void Draw(MySpriteDrawFrame frame)
            {
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
