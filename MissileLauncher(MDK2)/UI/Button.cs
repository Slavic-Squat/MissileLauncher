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
            public bool IsPressed => _state.HasFlag(ButtonState.Pressed);
            public bool IsHighlighted => _state.HasFlag(ButtonState.Highlighted);

            private Func<bool> _func;
            private ButtonState _state = ButtonState.None;
            private DateTime _timePressed = DateTime.MinValue;
            private Func<bool> _isPressed;

            private MySprite _baseSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;
            private MySprite _textSprite;

            [Flags]
            public enum ButtonState
            {
                None = 0, Highlighted = 1, Pressed = 1 << 1,
            }

            public Button(string name, Vector2 pos, Vector2 size, string text, float textScale, Func<bool> func, Func<bool> isPressed = null)
            {
                Name = name;
                Pos = pos;
                Size = size;
                _func = func;
                _isPressed = isPressed;

                _baseSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pos,
                    Size = size - 20,
                    Color = UIConfig.ButtonBackgroundColor,
                    Alignment = TextAlignment.CENTER
                };

                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pos,
                    Size = size,
                    Color = UIConfig.ButtonBorderColor,
                    Alignment = TextAlignment.CENTER
                };

                _highlightSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pos,
                    Size = size * 1.1f + 10f,
                    Color = UIConfig.ButtonHighlightColor,
                    Alignment = TextAlignment.CENTER
                };

                _textSprite = SpriteHelper.CreateText(pos, text, UIConfig.ButtonTextColor, textScale);
            }

            public void Press(DateTime updateTime)
            {
                _timePressed = updateTime;
                _func?.Invoke();
                SetPressed();
            }

            public void Release()
            {
                SetReleased();
            }

            private void SetPressed()
            {
                _state |= ButtonState.Pressed;
                _baseSprite.Color = UIConfig.ButtonBackgroundColorPressed;
                _borderSprite.Color = UIConfig.ButtonBorderColorPressed;
                _textSprite.Color = UIConfig.ButtonTextColorPressed;
            }

            private void SetReleased()
            {
                _state &= ~ButtonState.Pressed;
                _baseSprite.Color = UIConfig.ButtonBackgroundColor;
                _borderSprite.Color = UIConfig.ButtonBorderColor;
                _textSprite.Color = UIConfig.ButtonTextColor;
            }

            public void Highlight()
            {
                _state |= ButtonState.Highlighted;
                _baseSprite.Size *= 1.1f;
                _borderSprite.Size *= 1.1f;
                _textSprite.RotationOrScale *= 1.1f;
            }

            public void Unhighlight()
            {
                _state &= ~ButtonState.Highlighted;
                _baseSprite.Size /= 1.1f;
                _borderSprite.Size /= 1.1f;
                _textSprite.RotationOrScale /= 1.1f;
            }

            public void Update(DateTime updateTime)
            {
                bool isPressed = _isPressed?.Invoke() ?? false;

                if (isPressed)
                {
                    SetPressed();
                }
                else if (updateTime - _timePressed > TimeSpan.FromSeconds(1))
                {
                    SetReleased();
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                if (_state.HasFlag(ButtonState.Highlighted))
                {
                    frame.Add(_highlightSprite);
                }
                frame.Add(_borderSprite);
                frame.Add(_baseSprite);
                frame.Add(_textSprite);
            }
        }
    }
}
