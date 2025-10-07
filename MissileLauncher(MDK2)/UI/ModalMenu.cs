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
        public class ModalMenu : IMenu, IModal
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsOpen { get; private set; }
            public bool IsPaused { get; private set; }
            public bool IsNavigating { get; private set; }
            public bool CanClose => _closeCondition?.Invoke() ?? true;
            public event Action<IMenu> RequestClose;
            public event Action<INavigable> RequestStopNavigation;

            private Func<bool> _closeCondition;
            private RectangleF _bounds;
            private float _borderThickness;
            private List<IButton> _commonButtons = new List<IButton>();
            private List<IUpdatable> _commonUpdateables = new List<IUpdatable>();
            private List<IUIElement> _commonUiElements = new List<IUIElement>();
            private List<MenuPage> _pages = new List<MenuPage>();
            private int _currentPageIndex = 0;
            private IButton _highlightedButton;

            private IMyTextSurface _surface;
            private MySprite _obscureSprite;
            private MySprite _fillSprite;
            private Color _fillColor = UIConfig.PanelFillColor;
            private MySprite _borderSprite;
            private Color _borderColor = UIConfig.PanelBorderColor;

            private List<MySprite> _commonSprites = new List<MySprite>();

            public ModalMenu(Vector2 pos, Vector2 size, float borderThickness, Func<bool> closeCondition, IMyTextSurface surface)
            {
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _closeCondition = closeCondition;
                _surface = surface;

                BuildSprites();
            }

            private void BuildSprites()
            {
                _obscureSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = _surface.TextureSize * 0.5f,
                    Size = _surface.SurfaceSize,
                    RotationOrScale = 0f,
                    Color = new Color(0, 0, 0, 200),
                    Alignment = TextAlignment.CENTER,
                };
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
                _fillColor = UIConfig.MenuFillColor;
                _borderColor = UIConfig.MenuBorderColor;
                UnhighlightButton(_highlightedButton);
                BuildSprites();
            }

            public void ResumeNavigation()
            {
                IsPaused = false;
                _fillColor = UIConfig.MenuFillColorActive;
                _borderColor = UIConfig.MenuBorderColorActive;
                BuildSprites();
            }

            public void AddButton(IButton button, int pageIndex)
            {
                if (pageIndex < 0)
                {
                    _commonButtons.Add(button);
                    _commonUpdateables.Add(button);
                    _commonUiElements.Add(button);
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
                            MenuPage newPage = new MenuPage(i);
                            _pages.Add(newPage);
                        }
                    }
                    var page = _pages[pageIndex];
                    page.Sprites.Add(sprite);
                }
            }

            public void AddInfoPanel(InfoPanel panel, int pageIndex)
            {
                if (pageIndex < 0)
                {
                    _commonUpdateables.Add(panel);
                    _commonUiElements.Add(panel);
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

            private void ActivateButton(IButton button, DateTime time)
            {
                button?.Press(time);
            }

            public void Update(DateTime time)
            {
                foreach (var updateable in _commonUpdateables)
                {
                    updateable.Update(time);
                }

                foreach (var page in _pages)
                {
                    page.Updateables.ForEach(u => u.Update(time));
                }

                if (IsOpen && CanClose)
                {
                    Close();
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.Add(_obscureSprite);
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);

                foreach (var sprite in _commonSprites)
                {
                    frame.Add(sprite);
                }

                foreach (var uiElement in _commonUiElements)
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

                List<IButton> allButtons = new List<IButton>(_commonButtons);

                if (_pages.Count > 0)
                {
                    if (input.QRelease)
                    {
                        _currentPageIndex--;
                        _currentPageIndex = (int)MiscUtilities.LoopInRange(_currentPageIndex, 0, _pages.Count - 1);
                    }
                    else if (input.ERelease)
                    {
                        _currentPageIndex++;
                        _currentPageIndex = (int)MiscUtilities.LoopInRange(_currentPageIndex, 0, _pages.Count - 1);
                    }
                    var currentPage = _pages[_currentPageIndex];
                    allButtons.AddRange(currentPage.Buttons);
                }

                if (!allButtons.Contains(_highlightedButton))
                {
                    UnhighlightButton(_highlightedButton);
                    HighlightButton(allButtons.FirstOrDefault());
                }

                if (input.WRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(allButtons, _highlightedButton, NavigationDirection.Up);
                    HighlightButton(nextButton);
                }
                else if (input.SRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(allButtons, _highlightedButton, NavigationDirection.Down);
                    HighlightButton(nextButton);
                }
                else if (input.ARelease)
                {
                    IButton nextButton = UIUtilities.Navigate(allButtons, _highlightedButton, NavigationDirection.Left);
                    HighlightButton(nextButton);
                }
                else if (input.DRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(allButtons, _highlightedButton, NavigationDirection.Right);
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
