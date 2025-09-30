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
            public Vector2 Pos { get; private set; }
            public Vector2 Size { get; private set; }
            public bool CanPress => _canPress?.Invoke() ?? true;
            public bool IsHighlighted => _state.HasFlag(ButtonState.Highlighted);

            [Flags]
            public enum ButtonState
            {
                None = 0, Highlighted = 1, Pressed = 1 << 1, Disabled = 1 << 2, Errored = 1 << 3
            }

            private Func<bool> _action;
            private ButtonState _state = ButtonState.None;
            private DateTime _timePressed = DateTime.MinValue;
            private Func<bool> _canPress;
            private string _text;
            private float _textScale;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;
            private MySprite _textSprite;

            public Button(string name, Vector2 pos, Vector2 size, string text, float textScale, Func<bool> action, Func<bool> canPress = null)
            {
                Name = name;
                Pos = pos;
                Size = size;

                _text = text;
                _textScale = textScale;
                _action = action;
                _canPress = canPress;

                BuildSprites();
            }

            private void BuildSprites()
            {
                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos,
                    Size = Size - 20,
                    Color = UIConfig.ButtonFillColor,
                    Alignment = TextAlignment.CENTER
                };

                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos,
                    Size = Size,
                    Color = UIConfig.ButtonBorderColor,
                    Alignment = TextAlignment.CENTER
                };

                _highlightSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos,
                    Size = Size * 1.1f + 10f,
                    Color = UIConfig.ButtonHighlightColor,
                    Alignment = TextAlignment.CENTER
                };

                _textSprite = SpriteHelper.CreateText(Pos, _text, UIConfig.ButtonTextColor, _textScale);
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
                _fillSprite.Size *= 1.1f;
                _borderSprite.Size *= 1.1f;
                _textSprite.RotationOrScale *= 1.1f;
            }

            public void Unhighlight()
            {
                if (!IsHighlighted)
                    return;

                _state &= ~ButtonState.Highlighted;
                _fillSprite.Size /= 1.1f;
                _borderSprite.Size /= 1.1f;
                _textSprite.RotationOrScale /= 1.1f;
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
                    _fillSprite.Color = UIConfig.ButtonFillColorDisabled;
                    _borderSprite.Color = UIConfig.ButtonBorderColorDisabled;
                    _textSprite.Color = UIConfig.ButtonTextColorDisabled;
                }
                else if (_state.HasFlag(ButtonState.Errored))
                {
                    _fillSprite.Color = UIConfig.ButtonFillErrored;
                    _borderSprite.Color = UIConfig.ButtonBorderColorErrored;
                    _textSprite.Color = UIConfig.ButtonTextColorErrored;
                }
                else if (_state.HasFlag(ButtonState.Pressed))
                {
                    _fillSprite.Color = UIConfig.ButtonFillColorPressed;
                    _borderSprite.Color = UIConfig.ButtonBorderColorPressed;
                    _textSprite.Color = UIConfig.ButtonTextColorPressed;
                }
                else
                {
                    _fillSprite.Color = UIConfig.ButtonFillColor;
                    _borderSprite.Color = UIConfig.ButtonBorderColor;
                    _textSprite.Color = UIConfig.ButtonTextColor;
                }
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
