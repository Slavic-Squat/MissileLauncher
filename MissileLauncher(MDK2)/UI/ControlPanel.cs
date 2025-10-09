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
        public class ControlPanel : IPanel, INavigable, IUpdatable, IHighlightable
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsHighlighted { get; private set; }
            public bool IsNavigating { get; private set; }
            public bool IsPaused { get; private set; }
            public event Action<INavigable> RequestStopNavigation;


            private RectangleF _bounds;
            private float _borderThickness;
            private float _highlightThickness;
            private List<IButton> _buttons = new List<IButton>();
            private IButton _highlightedButton;

            private MySprite _fillSprite;
            private Color _fillColor = UIConfig.PanelFillColor;
            private MySprite _borderSprite;
            private Color _borderColor = UIConfig.PanelBorderColor;
            private MySprite _highlightSprite;
            private Color _highlightColor = UIConfig.PanelHighlightColor;

            private List<MySprite> _sprites = new List<MySprite>();

            public ControlPanel(Vector2 pos, Vector2 size, float borderThickness, float highlightThickness)
            {
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _highlightThickness = highlightThickness;

                BuildSprites();
            }

            public void BuildSprites()
            {
                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 2 * _borderThickness,
                    RotationOrScale = 0f,
                    Color = _fillColor,
                    Alignment = TextAlignment.CENTER,
                };
                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    RotationOrScale = 0f,
                    Color = _borderColor,
                    Alignment = TextAlignment.CENTER,
                };
                _highlightSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size + 2 * _highlightThickness,
                    Color = _highlightColor,
                    Alignment = TextAlignment.CENTER
                };
            }

            public void AddButton(IButton button)
            {
                _buttons.Add(button);
            }

            public void AddSprite(MySprite sprite)
            {
                _sprites.Add(sprite);
            }

            public void Highlight()
            {
                IsHighlighted = true;
            }

            public void Unhighlight()
            {
                IsHighlighted = false;
            }

            public void OnStartNavigation()
            {
                IsNavigating = true;
                ResumeNavigation();
            }

            private void StopNavigation()
            {
                RequestStopNavigation?.Invoke(this);
            }

            public void OnStopNavigation()
            {
                IsNavigating = false;
                PauseNavigation();
            }

            public void PauseNavigation()
            {
                IsPaused = true;
                UnhighlightButton(_highlightedButton);
                _fillColor = UIConfig.PanelFillColor;
                _borderColor = UIConfig.PanelBorderColor;
                BuildSprites();
            }

            public void ResumeNavigation()
            {
                IsPaused = false;
                _fillColor = UIConfig.PanelFillColorActive;
                _borderColor = UIConfig.PanelBorderColorActive;
                if (_buttons.Count > 0 && _highlightedButton == null)
                {
                    HighlightButton(_buttons[0]);
                }
                BuildSprites();
            }

            private void HighlightButton(IButton button)
            {
                if (button == null || ReferenceEquals(button, _highlightedButton))
                {
                    return;
                }
                UnhighlightButton(_highlightedButton);
                button.Highlight();
                _highlightedButton = button;
            }

            private void UnhighlightButton(IButton button)
            {
                button?.Unhighlight();

                if (ReferenceEquals(button, _highlightedButton))
                {
                    _highlightedButton = null;
                }
            }

            public void Update(DateTime time)
            {
                foreach (var button in _buttons)
                {
                    button.Update(time);
                }
            }

            private void ActivateButton(IButton button, DateTime time)
            {
                button?.Press(time);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                if (IsHighlighted && (IsPaused || !IsNavigating))
                {
                    frame.Add(_highlightSprite);
                }
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);

                foreach (var sprite in _sprites)
                {
                    frame.Add(sprite);
                }

                foreach (var button in _buttons)
                {
                    if (button == _highlightedButton)
                    {
                        continue;
                    }
                    button.Draw(frame);
                }
                _highlightedButton?.Draw(frame);
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (input.CRelease)
                {
                    StopNavigation();
                }

                if (_buttons.Count == 0)
                {
                    return;
                }

                if (input.WRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, Direction.Up);
                    HighlightButton(nextButton);
                }
                else if (input.SRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, Direction.Down);
                    HighlightButton(nextButton);
                }
                else if (input.ARelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, Direction.Left);
                    HighlightButton(nextButton);
                }
                else if (input.DRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, Direction.Right);
                    HighlightButton(nextButton);
                }
                else if (input.SpaceRelease)
                {
                    ActivateButton(_highlightedButton, time);
                }
            }
        }
    }
}
