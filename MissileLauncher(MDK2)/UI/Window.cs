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
        public class Window : IWindow
        {
            public UI UI { get; private set; }
            public DateTime Time { get; private set; }
            public object Parent { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsOpen { get; private set; } = false;
            public bool IsNavigating { get; private set; } = false;
            public bool IsPaused { get; private set; } = true;
            public event Func<IWindow, bool> RequestClose;
            public event Func<INavigable, bool> RequestStopNavigation;

            public IMyTextSurface Display => UI.Display;

            protected RectangleF _bounds;
            protected float _borderThickness;

            protected MySprite _fillSprite;
            protected MySprite _borderSprite;
            protected List<MySprite> _additionalSprites = new List<MySprite>();

            protected List<IHighlightable> _highlightables = new List<IHighlightable>();
            protected IHighlightable _highlightedElement;
            protected List<IUpdatable> _updatables = new List<IUpdatable>();
            protected List<IUIElement> _uiElements = new List<IUIElement>();
            protected List<INavigable> _navigables = new List<INavigable>();
            protected INavigable _navigatedElement;


            public Window(UI ui, Vector2 pos, Vector2 size, float borderThickness)
            {
                UI = ui;
                Parent = ui;

                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;

                Init();
            }

            public Window(UI ui, float borderThickness)
            {
                UI = ui;
                Parent = ui;

                _bounds = ui.Bounds;
                _borderThickness = borderThickness;

                Init();
            }

            private void Init()
            {

            }

            protected virtual void BuildSprites()
            {
                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 2 * _borderThickness,
                    Color = UIConfig.WindowFillColor,
                    Alignment = TextAlignment.CENTER
                };

                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    Color = UIConfig.WindowBorderColor,
                    Alignment = TextAlignment.CENTER
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

            public virtual bool ResumeNavigation()
            {
                IsPaused = false;
                return true;
            }

            public virtual bool PauseNavigation()
            {
                IsPaused = true;
                UnhighlightElement(_highlightedElement);
                return true;
            }

            public virtual bool AddButton(IButton button)
            {
                if (button == null) return false;
                _uiElements.Add(button);
                _updatables.Add(button);
                _highlightables.Add(button);
                return true;
            }

            public virtual bool AddSprite(MySprite sprite)
            {
                _additionalSprites.Add(sprite);
                return true;
            }

            public virtual bool AddInfoPanel(InfoPanel panel)
            {
                if (panel == null) return false;
                _uiElements.Add(panel);
                return true;
            }

            public virtual bool AddControlPanel(ControlPanel panel)
            {
                if (panel == null || !ReferenceEquals(this, panel.Parent)) return false;
                _uiElements.Add(panel);
                _updatables.Add(panel);
                _highlightables.Add(panel);
                return true;
            }

            protected virtual bool HighlightElement(IHighlightable highlightable)
            {
                if (highlightable == null || ReferenceEquals(highlightable, _highlightedElement))
                {
                    return false;
                }
                UnhighlightElement(_highlightedElement);
                highlightable.Highlight();
                _highlightedElement = highlightable;
                return true;
            }

            protected virtual bool UnhighlightElement(IHighlightable hightlightable)
            {
                if (hightlightable == null)
                {
                    return false;
                }
                hightlightable.Unhighlight();

                if (_highlightedElement == hightlightable)
                {
                    _highlightedElement = null;
                }
                return true;
            }

            protected virtual bool ActivateHighlightable(IHighlightable highlightable)
            {
                if (highlightable == null) return false;
                if (highlightable is IButton)
                {
                    ((IButton)highlightable).Press();
                }
                else if (highlightable is INavigable)
                {
                    StartNavigatingElement((INavigable)highlightable);
                }
                return true;
            }

            public virtual bool StartNavigatingElement(INavigable navigable)
            {
                if (navigable == null || ReferenceEquals(navigable, _navigatedElement) || !ReferenceEquals(this, navigable.Parent))
                {
                    return false;
                }
                _navigables.Add(navigable);
                navigable.StartNavigation(this);
                navigable.RequestStopNavigation += StopNavigatingElement;
                
                _navigatedElement?.PauseNavigation();
                _navigatedElement = navigable;
                return true;
            }

            public virtual bool StopNavigatingElement(INavigable navigable)
            {
                if (navigable == null || !ReferenceEquals(this, navigable.Parent))
                {
                    return false;
                }

                navigable.StopNavigation(this);
                navigable.RequestStopNavigation -= StopNavigatingElement;
                _navigables.Remove(navigable);

                if (ReferenceEquals(navigable, _navigatedElement))
                {
                    _navigatedElement = null;
                    if (_navigables.Count > 0)
                    {
                        _navigatedElement = _navigables.Last();
                        _navigatedElement.ResumeNavigation();
                    }
                }
                return true;
            }

            public virtual bool OpenMenu(IMenu menu)
            {
                if (menu == null || !ReferenceEquals(this, menu.Parent)) return false;
                _updatables.Add(menu);
                _uiElements.Add(menu);

                menu.Open(this);
                menu.RequestClose += CloseMenu;

                StartNavigatingElement(menu);
                return true;
            }

            public virtual bool CloseMenu(IMenu menu)
            {
                if (menu == null || !ReferenceEquals(this, menu.Parent)) return false;
                _updatables.Remove(menu);
                _uiElements.Remove(menu);

                StopNavigatingElement(menu);

                menu.RequestClose -= CloseMenu;
                menu.Close(this);
                return true;
            }

            public virtual bool Update(DateTime time)
            {
                if (!IsOpen) return false;
                Time = time;
                foreach (var updatable in _updatables.ToList())
                {
                    updatable.Update(time);
                }
                return true;
            }

            public virtual bool Draw(MySpriteDrawFrame frame)
            {
                if (!IsOpen) return false;
                BuildSprites();
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);

                foreach (var sprite in _additionalSprites)
                {
                    frame.Add(sprite);
                }

                foreach (var element in _uiElements)
                {
                    element.Draw(frame);

                    if (ReferenceEquals(element, _navigatedElement) || ReferenceEquals(element, _highlightedElement))
                    {
                        continue;
                    }
                }

                if (_navigatedElement != null)
                {
                    _navigatedElement.Draw(frame);
                }
                else
                {
                    _highlightedElement?.Draw(frame);
                }

                return true;
            }

            public virtual bool Navigate(UserInput input, object caller)
            {
                if (!IsOpen || !IsNavigating || IsPaused || !ReferenceEquals(Parent, caller))
                {
                    return false;
                }
                if (_navigatedElement != null)
                {
                    return _navigatedElement.Navigate(input, this);
                }

                if (input.CRelease)
                {
                    Close();
                    return false;
                }

                if (_highlightables.Count == 0)
                {
                    return false;
                }

                if (!_highlightables.Contains(_highlightedElement))
                {
                    UnhighlightElement(_highlightedElement);
                    HighlightElement(_highlightables[0]);
                }

                if (input.WRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, Direction.Up);
                    HighlightElement(nextElement);
                }
                else if (input.SRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, Direction.Down);
                    HighlightElement(nextElement);
                }
                else if (input.ARelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, Direction.Left);
                    HighlightElement(nextElement);
                }
                else if (input.DRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, Direction.Right);
                    HighlightElement(nextElement);
                }
                else if (input.SpaceRelease)
                {
                    ActivateHighlightable(_highlightedElement);
                }
                return true;
            }
        }
    }
}
