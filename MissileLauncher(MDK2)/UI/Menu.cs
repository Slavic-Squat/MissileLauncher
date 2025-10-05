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
        public class Menu : IMenu
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsOpen { get; private set; }
            public bool IsPaused { get; private set; }
            public bool IsNavigating { get; private set; }
            public event Action<IMenu> RequestClose;
            public event Action<INavigable> RequestStopNavigation;


            private RectangleF _bounds;
            private List<IButton> _buttons = new List<IButton>();
            private IButton _highlightedButton;

            private MySprite _fillSprite;
            private Color _fillColor = UIConfig.PanelFillColor;
            private MySprite _borderSprite;
            private Color _borderColor = UIConfig.PanelBorderColor;

            private List<MySprite> _sprites = new List<MySprite>();

            public Menu(Vector2 pos, Vector2 size)
            {
                _bounds = new RectangleF(pos, size);
                Open();

                BuildSprites();
            }

            public void BuildSprites()
            {
                float minDim = Math.Min(_bounds.Width, _bounds.Height);

                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 0.5f * 0.1f * minDim,
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
            }

            public void Open()
            {
                IsOpen = true;
                StartNavigation();
            }

            private void Close()
            {
                RequestClose?.Invoke(this);
                OnClose();
            }

            public void OnClose()
            {
                IsOpen = false;
                StopNavigation();
            }

            public void StartNavigation()
            {
                IsNavigating = true;
                ResumeNavigation();
            }

            private void StopNavigation()
            {
                RequestStopNavigation?.Invoke(this);
                OnStopNavigation();
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
                _fillColor = UIConfig.MenuFillColor;
                _borderColor = UIConfig.MenuBorderColor;
                BuildSprites();
            }

            public void ResumeNavigation()
            {
                IsPaused = false;
                _fillColor = UIConfig.MenuFillColorActive;
                _borderColor = UIConfig.MenuBorderColorActive;
                if (_buttons.Count > 0)
                {
                    HighlightButton(_buttons[0]);
                }
                BuildSprites();
            }

            public void AddButton(IButton button)
            {
                _buttons.Add(button);
            }

            public void AddSprite(MySprite sprite)
            {
                _sprites.Add(sprite);
            }

            private void HighlightButton(IButton button)
            {
                UnhighlightButton(_highlightedButton);
                button.Highlight();
                _highlightedButton = button;
            }

            private void UnhighlightButton(IButton button)
            {
                button?.Unhighlight();

                if (_highlightedButton == button)
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
                if (!IsNavigating || IsPaused)
                {
                    return;
                }
                if (input.CRelease)
                {
                    Close();
                }
                if (_buttons.Count == 0)
                {
                    return;
                }

                if (input.WRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, NavigationDirection.Up);
                    HighlightButton(nextButton);
                }
                else if (input.SRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, NavigationDirection.Down);
                    HighlightButton(nextButton);
                }
                else if (input.ARelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, NavigationDirection.Left);
                    HighlightButton(nextButton);
                }
                else if (input.DRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, NavigationDirection.Right);
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
