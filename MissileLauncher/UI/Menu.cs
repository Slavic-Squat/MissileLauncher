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
            public object Parent { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsOpen { get; private set; } = false;
            public bool IsPaused { get; private set; } = true;
            public bool IsNavigating { get; private set; } = false;
            public event Action<IMenu> RequestClose;
            public event Action<INavigable> RequestStopNavigation;

            protected double _time;
            protected bool _canUserClose;
            protected Func<bool> _autoClose;
            protected RectangleF _bounds;
            protected float _borderThickness;
            protected List<IButton> _commonButtons = new List<IButton>();
            protected List<IUpdatable> _commonUpdatables = new List<IUpdatable>();
            protected List<IUIElement> _commonUIElements = new List<IUIElement>();
            protected List<MenuPage> _pages = new List<MenuPage>();
            protected int _currentPageIndex = 0;
            protected IButton _highlightedButton;

            protected bool _obscure;
            protected RectangleF _screenBounds;

            protected MySprite[] _bodySprites;
            protected MySprite _obscureSprite;

            protected List<MySprite> _commonSprites = new List<MySprite>();

            public Menu(object parent, Vector2 pos, Vector2 size, float borderThickness, bool obscure = false, Func<bool> autoClose = null, bool canUserClose = true, RectangleF screenBounds = default(RectangleF))
            {
                Parent = parent;
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _obscure = obscure;
                _autoClose = autoClose;
                _canUserClose = canUserClose;

                if (screenBounds == default(RectangleF))
                {
                    _screenBounds = new RectangleF(0, 0, 1024f, 1024f);
                }
                else
                {
                    _screenBounds = screenBounds;
                }
            }

            protected virtual void BuildSprites()
            {
                Color fillColor, borderColor;
                if (IsPaused)
                {
                    fillColor = UIConfig.MenuFillColor;
                    borderColor = UIConfig.MenuBorderColor;
                }
                else
                {
                    fillColor = UIConfig.MenuFillColorActive;
                    borderColor = UIConfig.MenuBorderColorActive;
                }

                if (_obscure)
                {
                    _obscureSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = _screenBounds.Center,
                        Size = _screenBounds.Size,
                        RotationOrScale = 0f,
                        Color = new Color(0, 0, 0, 229),
                        Alignment = TextAlignment.CENTER,
                    };
                }
                _bodySprites = SpriteHelper.CreateBoxFilled(Bounds, borderColor, fillColor, _borderThickness);
            }

            public virtual void Open(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsOpen = true;
            }

            protected virtual void Close()
            {
                if (!_canUserClose) return;
                RequestClose?.Invoke(this);
            }

            public virtual void Close(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsOpen = false;
            }

            public virtual void StartNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsNavigating = true;
                ResumeNavigation(caller);
            }

            protected virtual void StopNavigation()
            {
                RequestStopNavigation?.Invoke(this);
            }

            public virtual void StopNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsNavigating = false;
                PauseNavigation(caller);
            }

            public virtual void PauseNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsPaused = true;
                UnhighlightButton(_highlightedButton);
            }

            public virtual void ResumeNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsPaused = false;
            }

            public virtual void AddButton(IButton button, int pageIndex)
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
                            MenuPage newPage = new MenuPage(i);
                            _pages.Add(newPage);
                        }
                    }
                    var page = _pages[pageIndex];
                    page.AddButton(button);
                }
            }

            public virtual void AddSprite(MySprite sprite, int pageIndex)
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
                            MenuPage newPage = new MenuPage(i);
                            _pages.Add(newPage);
                        }
                    }
                    var page = _pages[pageIndex];
                    page.Sprites.Add(sprite);
                }
            }

            public virtual void AddInfoPanel(InfoPanel panel, int pageIndex)
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
                            MenuPage newPage = new MenuPage(i);
                            _pages.Add(newPage);
                        }
                    }
                    var page = _pages[pageIndex];
                    page.AddInfoPanel(panel);
                }
            }

            protected virtual void HighlightButton(IButton button)
            {
                if (button == null || ReferenceEquals(button, _highlightedButton))
                {
                    return;
                }
                UnhighlightButton(_highlightedButton);
                button.Highlight();
                _highlightedButton = button;
            }

            protected virtual void UnhighlightButton(IButton button)
            {
                if (button == null) return;
                button.Unhighlight();

                if (ReferenceEquals(button, _highlightedButton))
                {
                    _highlightedButton = null;
                }
            }

            public virtual void Update(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                if (!IsOpen) return;

                if (_autoClose?.Invoke() ?? false)
                {
                    Close();
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

            protected virtual void ActivateButton(IButton button)
            {
                if (button == null) return;
                button.Press();
            }

            public virtual void Draw(MySpriteDrawFrame frame)
            {
                if (!IsOpen || !IsNavigating || IsPaused) return;
                BuildSprites();
                if (_obscure)
                {
                    frame.Add(_obscureSprite);
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

            public virtual void Navigate(UserInput input, object caller)
            {
                if (!IsOpen || !IsNavigating || IsPaused || !ReferenceEquals(Parent, caller))
                {
                    return;
                }
                if (input.CRelease)
                {
                    Close();
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
