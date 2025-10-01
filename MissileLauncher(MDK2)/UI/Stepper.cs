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
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => Bounds.Position;
            public Vector2 Size => Bounds.Size;
            public Vector2 Center => Bounds.Center;
            public T CurrentState => _stateGetter();
            public bool IsHighlighted { get; private set; }
            public bool IsInside { get; private set; }

            private RectangleF _bounds;
            private readonly Vector2 _originalPos;
            private readonly Vector2 _originalSize;

            private Func<T> _stateGetter;
            private Action _onForward;
            private Action _onBackward;
            private Dictionary<T, string> _displayNames;

            private Color _fillColor = UIConfig.ButtonFillColor;
            private Color _borderColor = UIConfig.ButtonBorderColor;
            private Color _textColor = UIConfig.ButtonTextColor;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;
            private MySprite _textSprite;

            private IMyTextSurface _surface;

            public Stepper(Vector2 pos, Vector2 size, Func<T> stateGetter, Action onForward, Action onBackward, Dictionary<T, string> displayNames, IMyTextSurface surface)
            {
                _bounds = new RectangleF(pos, size);
                _originalPos = pos;
                _originalSize = size;

                _stateGetter = stateGetter;
                _onForward = onForward;
                _onBackward = onBackward;
                _displayNames = displayNames;
                _surface = surface;

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

                string displayName = _displayNames.ContainsKey(CurrentState) ? _displayNames[CurrentState] : "N/A";
                _textSprite = SpriteHelper.CreateText(Bounds, displayName, _textColor, _surface, TextAlignment.CENTER, true, 0.75f);
            }

            private void OnStep()
            {
                string displayName = _displayNames.ContainsKey(CurrentState) ? _displayNames[CurrentState] : "N/A";
                _textSprite = SpriteHelper.CreateText(Bounds, displayName, _textColor, _surface, TextAlignment.CENTER, true, 0.75f);
                BuildSprites();
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
                _bounds.Size = _originalSize * 1.1f;
                _bounds.Position -= _originalSize * 0.05f;

                BuildSprites();
            }

            public void Unhighlight()
            {
                if (!IsHighlighted)
                    return;

                IsHighlighted = false;
                _bounds.Size = _originalSize;
                _bounds.Position = _originalPos;

                BuildSprites();
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
