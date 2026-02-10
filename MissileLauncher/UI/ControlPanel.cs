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
            public object Parent { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsHighlighted { get; private set; } = false;
            public bool IsNavigating { get; private set; } = false;
            public bool IsPaused { get; private set; } = true;
            public event Action<INavigable> RequestStopNavigation;

            private double _time;
            private RectangleF _bounds;
            private float _borderThickness;
            private float _highlightThickness;
            private List<IButton> _commonButtons = new List<IButton>(8);
            private List<IUpdatable> _commonUpdatables = new List<IUpdatable>(8);
            private List<IUIElement> _commonUIElements = new List<IUIElement>(16);
            private List<PanelPage> _pages = new List<PanelPage>(8);
            private int _currentPageIndex = 0;
            private IButton _highlightedButton;

            private List<MySprite> _bodySprites = new List<MySprite>(8);
            private MySprite _highlightSprite;

            private List<MySprite> _commonSprites = new List<MySprite>(32);

            public ControlPanel(object parent, Vector2 pos, Vector2 size, float borderThickness, float highlightThickness)
            {
                Parent = parent;
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _highlightThickness = highlightThickness;
            }

            private void BuildSprites()
            {
                Color fillColor, borderColor;
                if (IsPaused)
                {
                    fillColor = IsHighlighted ? UIConfig.PanelFillColorHighlighted : UIConfig.PanelFillColor;
                    borderColor = UIConfig.PanelBorderColor;
                }
                else
                {
                    fillColor = UIConfig.PanelFillColorActive;
                    borderColor = UIConfig.PanelBorderColorActive;
                }

                _bodySprites.Clear();
                SpriteHelper.CreateBoxFilled(_bodySprites, _bounds, borderColor, fillColor, _borderThickness);
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

            public void StartNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsNavigating = true;
                ResumeNavigation(caller);
            }

            private void StopNavigation()
            {
                RequestStopNavigation?.Invoke(this);
            }

            public void StopNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsNavigating = false;
                PauseNavigation(caller);
            }

            public void PauseNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsPaused = true;
                UnhighlightButton(_highlightedButton);
            }

            public void ResumeNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
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

            public void Update(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                foreach (var updatable in _commonUpdatables)
                {
                    updatable.Update(time);
                }

                foreach (var page in _pages)
                {
                    page.Updateables.ForEach(u => u.Update(time));
                }
                _time = time;
            }

            private void ActivateButton(IButton button)
            {
                if (button == null) return;
                button.Press();
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                if (IsHighlighted && (IsPaused || !IsNavigating))
                {
                    frame.Add(_highlightSprite);
                }
                frame.AddRange(_bodySprites);

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

            public void Navigate(UserInput input, object caller)
            {
                if (!IsNavigating || IsPaused || !ReferenceEquals(Parent, caller))
                {
                    return;
                }

                if (input.CRelease)
                {
                    StopNavigation();
                    return;
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
                    ActivateButton(_highlightedButton);
                }
            }
        }
    }
}
