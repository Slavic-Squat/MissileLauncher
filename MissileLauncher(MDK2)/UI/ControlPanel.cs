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
            public bool IsHighlighted { get; private set; } = false;
            public bool IsNavigating { get; private set; } = false;
            public bool IsPaused { get; private set; } = true;
            public event Action<INavigable> RequestStopNavigation;


            private RectangleF _bounds;
            private float _borderThickness;
            private float _highlightThickness;
            private List<IButton> _commonButtons = new List<IButton>();
            private List<IUpdatable> _commonUpdatables = new List<IUpdatable>();
            private List<IUIElement> _commonUIElements = new List<IUIElement>();
            private List<PanelPage> _pages = new List<PanelPage>();
            private int _currentPageIndex = 0;
            private IButton _highlightedButton;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;

            private List<MySprite> _commonSprites = new List<MySprite>();

            public ControlPanel(Vector2 pos, Vector2 size, float borderThickness, float highlightThickness)
            {
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _highlightThickness = highlightThickness;
            }

            private void BuildSprites()
            {
                Color fillColor, borderColor;
                if (IsPaused)
                {
                    fillColor = UIConfig.PanelFillColor;
                    borderColor = UIConfig.PanelBorderColor;
                }
                else
                {
                    fillColor = UIConfig.PanelFillColorActive;
                    borderColor = UIConfig.PanelBorderColorActive;
                }
                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 2 * _borderThickness,
                    RotationOrScale = 0f,
                    Color = fillColor,
                    Alignment = TextAlignment.CENTER,
                };
                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    RotationOrScale = 0f,
                    Color = borderColor,
                    Alignment = TextAlignment.CENTER,
                };
                _highlightSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size + 2 * _highlightThickness,
                    Color = UIConfig.PanelHighlightColor,
                    Alignment = TextAlignment.CENTER
                };
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
            }

            public void ResumeNavigation()
            {
                IsPaused = false;
            }

            public void AddButton(IButton button, int pageIndex)
            {
                if (button == null) return;
                if (pageIndex < 0)
                {
                    _commonButtons.Add(button);
                    _commonUpdatables.Add(button);
                    _commonUIElements.Add(button);
                    return;
                }
                else
                {
                    if (pageIndex >= _pages.Count)
                    {
                        for (int i = _pages.Count; i <= pageIndex; i++)
                        {
                            PanelPage newPage = new PanelPage(i);
                            _pages.Add(newPage);
                        }
                    }
                    var page = _pages[pageIndex];
                    page.AddButton(button);
                }
            }

            public void AddSprite(MySprite sprite, int pageIndex)
            {
                if (pageIndex < 0)
                {
                    _commonSprites.Add(sprite);
                    return;
                }
                else
                {
                    if (pageIndex >= _pages.Count)
                    {
                        for (int i = _pages.Count; i <= pageIndex; i++)
                        {
                            PanelPage newPage = new PanelPage(i);
                            _pages.Add(newPage);
                        }
                    }
                    var page = _pages[pageIndex];
                    page.Sprites.Add(sprite);
                }
            }

            public void AddInfoPanel(InfoPanel panel, int pageIndex)
            {
                if (panel == null) return;
                if (pageIndex < 0)
                {
                    _commonUIElements.Add(panel);
                    return;
                }
                else
                {
                    if (pageIndex >= _pages.Count)
                    {
                        for (int i = _pages.Count; i <= pageIndex; i++)
                        {
                            PanelPage newPage = new PanelPage(i);
                            _pages.Add(newPage);
                        }
                    }
                    var page = _pages[pageIndex];
                    page.AddInfoPanel(panel);
                }
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
                if (button == null) return;
                button.Unhighlight();

                if (ReferenceEquals(button, _highlightedButton))
                {
                    _highlightedButton = null;
                }
            }

            public void Update(DateTime time)
            {
                foreach (var updatable in _commonUpdatables)
                {
                    updatable.Update(time);
                }

                foreach (var page in _pages)
                {
                    page.Updateables.ForEach(u => u.Update(time));
                }
            }

            private void ActivateButton(IButton button, DateTime time)
            {
                if (button == null) return;
                button.Press(time);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                if (IsHighlighted && (IsPaused || !IsNavigating))
                {
                    frame.Add(_highlightSprite);
                }
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);

                foreach (var sprite in _commonSprites)
                {
                    frame.Add(sprite);
                }

                foreach (var uiElement in _commonUIElements)
                {
                    uiElement.Draw(frame);
                }

                if (_pages.Count > 0 && _pages.Count > _currentPageIndex)
                {
                    var currentPage = _pages[_currentPageIndex];
                    currentPage.Sprites.ForEach(s => frame.Add(s));
                    currentPage.UIElements.ForEach(e => e.Draw(frame));
                }
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (!IsNavigating || IsPaused)
                {
                    return;
                }

                if (input.CRelease)
                {
                    StopNavigation();
                }

                List<IButton> allButtons = new List<IButton>(_commonButtons);

                if (_pages.Count > 0)
                {
                    if (input.QRelease)
                    {
                        _currentPageIndex--;
                        _currentPageIndex = MiscUtilities.LoopInRange(_currentPageIndex, 0, _pages.Count);
                    }
                    else if (input.ERelease)
                    {
                        _currentPageIndex++;
                        _currentPageIndex = MiscUtilities.LoopInRange(_currentPageIndex, 0, _pages.Count);
                    }
                    var currentPage = _pages[_currentPageIndex];
                    allButtons.AddRange(currentPage.Buttons);
                }

                if (allButtons.Count == 0)
                {
                    return;
                }

                if (!allButtons.Contains(_highlightedButton))
                {
                    UnhighlightButton(_highlightedButton);
                    HighlightButton(allButtons[0]);
                }

                if (input.WRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(allButtons, _highlightedButton, Direction.Up);
                    HighlightButton(nextButton);
                }
                else if (input.SRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(allButtons, _highlightedButton, Direction.Down);
                    HighlightButton(nextButton);
                }
                else if (input.ARelease)
                {
                    IButton nextButton = UIUtilities.Navigate(allButtons, _highlightedButton, Direction.Left);
                    HighlightButton(nextButton);
                }
                else if (input.DRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(allButtons, _highlightedButton, Direction.Right);
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
