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
        public class ToggleButton : IButton
        {
            public string Name { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => Bounds.Position;
            public Vector2 Size => Bounds.Size;
            public Vector2 Center => Bounds.Center;
            public bool CanPress
            {
                get
                {
                    if (_state.HasFlag(ButtonState.Pressed))
                    {
                        return _canRelease?.Invoke() ?? true;
                    }
                    else
                    {
                        return _canPress?.Invoke() ?? true;
                    }
                }
            }
            public bool IsHighlighted => _state.HasFlag(ButtonState.Highlighted);

            [Flags]
            public enum ButtonState
            {
                None = 0, Highlighted = 1, Pressed = 1 << 1, Disabled = 1 << 2, Errored = 1 << 3
            }

            private RectangleF _bounds;
            private readonly Vector2 _originalPos;
            private readonly Vector2 _originalSize;

            private float _padding;
            private float _borderThickness;
            private float _highlightThickness;

            private Func<bool> _onPress;
            private Func<bool> _onRelease;
            private Func<bool> _isPressed;
            private ButtonState _state = ButtonState.None;
            private DateTime _timePressed = DateTime.MinValue;
            private Func<bool> _canPress;
            private Func<bool> _canRelease;
            private Func<string> _textGetter;

            private Color _fillColor = UIConfig.ButtonFillColor;
            private Color _borderColor = UIConfig.ButtonBorderColor;
            private Color _textColor = UIConfig.ButtonTextColor;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;
            private MySprite _textSprite;

            private IMyTextSurface _surface;

            public ToggleButton(string name, Vector2 pos, Vector2 size, float padding, float borderThickness, float highlightThickness, Func<string> textGetter, Func<bool> onPress, Func<bool> onRelease, Func<bool> isPressed, IMyTextSurface surface, Func<bool> canPress = null, Func<bool> canRelease = null)
            {
                Name = name;

                _bounds = new RectangleF(pos, size);
                _originalPos = pos;
                _originalSize = size;
                _padding = padding;
                _borderThickness = borderThickness;
                _highlightThickness = highlightThickness;
                _onPress = onPress;
                _onRelease = onRelease;
                _isPressed = isPressed;

                _textGetter = textGetter;
                _surface = surface;
                _canPress = canPress;
                _canRelease = canRelease;

                BuildSprites();
            }

            private void BuildSprites()
            {
                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 2 * _borderThickness,
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
                    Size = Size + 2 * _highlightThickness,
                    Color = UIConfig.ButtonHighlightColor,
                    Alignment = TextAlignment.CENTER
                };

                _textSprite = SpriteHelper.CreateText(Bounds, _textGetter(), _textColor, _surface, TextAlignment.CENTER, true, _padding);
            }

            public void Press(DateTime time)
            {
                if (!CanPress)
                {
                    return;
                }

                _timePressed = time;

                if (_state.HasFlag(ButtonState.Pressed))
                {
                    if (_onRelease?.Invoke() == false)
                    {
                        _state |= ButtonState.Errored;
                        return;
                    }
                }
                else
                {
                    if (_onPress?.Invoke() == false)
                    {
                        _state |= ButtonState.Errored;
                        return;
                    }
                }
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
                if (_isPressed?.Invoke() == true)
                {
                    _state |= ButtonState.Pressed;
                }
                else
                {
                    _state &= ~ButtonState.Pressed;
                }

                if (time - _timePressed > TimeSpan.FromSeconds(2) && _state.HasFlag(ButtonState.Errored))
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
                    _fillColor = UIConfig.ToggleButtonFillColorPressed;
                    _borderColor = UIConfig.ToggleButtonBorderColorPressed;
                    _textColor = UIConfig.ToggleButtonTextColorPressed;
                }
                else
                {
                    _fillColor = UIConfig.ToggleButtonFillColorReleased;
                    _borderColor = UIConfig.ToggleButtonBorderColorReleased;
                    _textColor = UIConfig.ToggleButtonTextColorReleased;
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
