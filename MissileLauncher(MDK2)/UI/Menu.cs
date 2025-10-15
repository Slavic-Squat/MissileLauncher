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
            public DateTime Time { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsOpen { get; private set; } = false;
            public bool IsPaused { get; private set; } = true;
            public bool IsNavigating { get; private set; } = false;
            public event Func<IMenu, bool> RequestClose;
            public event Func<INavigable, bool> RequestStopNavigation;

            protected Func<bool> _autoClose;
            protected RectangleF _bounds;
            protected float _borderThickness;
            protected List<IButton> _commonButtons = new List<IButton>();
            protected List<IUpdatable> _commonUpdatables = new List<IUpdatable>();
            protected List<IUIElement> _commonUIElements = new List<IUIElement>();
            protected List<MenuPage> _pages = new List<MenuPage>();
            protected int _currentPageIndex = 0;
            protected IButton _highlightedButton;

            protected IMyTextSurface _surface;
            protected bool _obscure;

            protected MySprite _fillSprite;
            protected MySprite _borderSprite;
            protected MySprite _obscureSprite;

            protected List<MySprite> _commonSprites = new List<MySprite>();

            public Menu(object parent, Vector2 pos, Vector2 size, float borderThickness, bool obscure = false, IMyTextSurface surface = null, Func<bool> autoClose = null)
            {
                Parent = parent;
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _obscure = obscure;
                _surface = surface;
                _autoClose = autoClose;
                if (_surface == null && obscure)
                {
                    throw new ArgumentException("----------------------\nSurface must be provided if obscure is true.\n-------------------------");
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
                        Position = _surface.TextureSize * 0.5f,
                        Size = _surface.SurfaceSize,
                        RotationOrScale = 0f,
                        Color = new Color(0, 0, 0, 229),
                        Alignment = TextAlignment.CENTER,
                    };
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
            }

            public virtual bool Open(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return false;
                }
                IsOpen = true;
                return true;
            }

            protected virtual bool Close()
            {
                return RequestClose?.Invoke(this) ?? false;
            }

            public virtual bool Close(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return false;
                }
                IsOpen = false;
                return true;
            }

            public virtual bool StartNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return false;
                }
                IsNavigating = true;
                ResumeNavigation();
                return true;
            }

            protected virtual bool StopNavigation()
            {
                return RequestStopNavigation?.Invoke(this) ?? false;
            }

            public virtual bool StopNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return false;
                }
                IsNavigating = false;
                PauseNavigation();
                return true;
            }
            
            public virtual bool PauseNavigation()
            {
                IsPaused = true;
                UnhighlightButton(_highlightedButton);
                return true;
            }

            public virtual bool ResumeNavigation()
            {
                IsPaused = false;
                return true;
            }

            public virtual bool AddButton(IButton button, int pageIndex)
            {
                if (button == null) return false;
                if (pageIndex < 0)
                {
                    _commonButtons.Add(button);
                    _commonUpdatables.Add(button);
                    _commonUIElements.Add(button);
                    return true;
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
                return true;
            }

            public virtual bool AddSprite(MySprite sprite, int pageIndex)
            {
                if (pageIndex < 0)
                {
                    _commonSprites.Add(sprite);
                    return true;
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
                return true;
            }

            public virtual bool AddInfoPanel(InfoPanel panel, int pageIndex)
            {
                if (panel == null) return true;
                if (pageIndex < 0)
                {
                    _commonUIElements.Add(panel);
                    return true;
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
                return true;
            }

            protected virtual bool HighlightButton(IButton button)
            {
                if (button == null || ReferenceEquals(button, _highlightedButton))
                {
                    return false;
                }
                UnhighlightButton(_highlightedButton);
                button.Highlight();
                _highlightedButton = button;
                return true;
            }

            protected virtual bool UnhighlightButton(IButton button)
            {
                if (button == null) return false;
                button.Unhighlight();

                if (ReferenceEquals(button, _highlightedButton))
                {
                    _highlightedButton = null;
                }
                return true;
            }

            public virtual bool Update(DateTime time)
            {
                if (!IsOpen) return false;

                Time = time;
                if (_autoClose?.Invoke() ?? false)
                {
                    Close();
                    return false;
                }
                foreach (var updatable in _commonUpdatables)
                {
                    updatable.Update(time);
                }

                foreach (var page in _pages)
                {
                    page.Updateables.ForEach(u => u.Update(time));
                }
                return true;
            }

            protected virtual bool ActivateButton(IButton button)
            {
                if (button == null) return false;
                return button.Press();
            }

            public virtual bool Draw(MySpriteDrawFrame frame)
            {
                if (!IsOpen || !IsNavigating || IsPaused) return false;
                BuildSprites();
                if (_obscure)
                {
                    frame.Add(_obscureSprite);
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
                return true;
            }

            public virtual bool Navigate(UserInput input, object caller)
            {
                if (!IsOpen || !IsNavigating || IsPaused || !ReferenceEquals(Parent, caller))
                {
                    return false;
                }
                if (input.CRelease)
                {
                    Close();
                    return false;
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
                    return false;
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
                return true;
            }
        }
    }
}
