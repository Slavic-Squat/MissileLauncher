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
        public class Stepper<T> : IStepper<T>
        {
            public Vector2 Pos { get; private set; }
            public Vector2 Size { get; private set; }
            public T CurrentState => _stateGetter();
            public bool IsHighlighted { get; private set; }
            public bool IsInside { get; private set; }

            private Func<T> _stateGetter;
            private Action _onForward;
            private Action _onBackward;
            private Dictionary<T, string> _displayNames;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;
            private MySprite _textSprite;

            public Stepper(Vector2 pos, Vector2 size, Func<T> stateGetter, Action onForward, Action onBackward, Dictionary<T, string> displayNames)
            {
                Pos = pos;
                Size = size;
                _stateGetter = stateGetter;
                _onForward = onForward;
                _onBackward = onBackward;
                _displayNames = displayNames;

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

                string displayName = _displayNames.ContainsKey(CurrentState) ? _displayNames[CurrentState] : "N/A";
                _textSprite = SpriteHelper.CreateText(Pos, displayName, UIConfig.ButtonTextColor, 1.0f);
            }

            private void OnStep()
            {
                string displayName = _displayNames.ContainsKey(CurrentState) ? _displayNames[CurrentState] : "N/A";
                _textSprite = SpriteHelper.CreateText(Pos, displayName, UIConfig.ButtonTextColor, 1.0f);
            }

            public void StepForward()
            {
                _onForward();
                OnStep();
            }

            public void StepBackward()
            {
                _onBackward();
                OnStep();
            }

            public void Enter()
            {
                IsInside = true;
            }

            public void Exit()
            {
                IsInside = false;
            }

            public void Highlight()
            {
                if (IsHighlighted)
                    return;

                IsHighlighted = true;
                _fillSprite.Size *= 1.1f;
                _borderSprite.Size *= 1.1f;
                _textSprite.RotationOrScale *= 1.1f;
            }

            public void Unhighlight()
            {
                if (!IsHighlighted)
                    return;

                IsHighlighted = false;
                _fillSprite.Size /= 1.1f;
                _borderSprite.Size /= 1.1f;
                _textSprite.RotationOrScale /= 1.1f;
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (input.CRelease)
                {
                    Exit();
                }
                else if (input.ARelease)
                {
                    StepBackward();
                }
                else if (input.DRelease)
                {
                    StepForward();
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
