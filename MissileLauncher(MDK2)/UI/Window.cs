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
            public event Action<IWindow> RequestClose;
            public event Action<INavigable> RequestStopNavigation;

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
                ResumeNavigation();
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
                PauseNavigation();
            }

            public virtual void ResumeNavigation()
            {
                IsPaused = false;
            }

            public virtual void PauseNavigation()
            {
                IsPaused = true;
                UnhighlightElement(_highlightedElement);
            }

            public virtual void AddButton(IButton button)
            {
                if (button == null) return;
                _uiElements.Add(button);
                _updatables.Add(button);
                _highlightables.Add(button);
            }

            public virtual void AddSprite(MySprite sprite)
            {
                _additionalSprites.Add(sprite);
            }

            public virtual void AddInfoPanel(InfoPanel panel)
            {
                if (panel == null) return;
                _uiElements.Add(panel);
            }

            public virtual void AddControlPanel(ControlPanel panel)
            {
                if (panel == null || !ReferenceEquals(this, panel.Parent)) return;
                _uiElements.Add(panel);
                _updatables.Add(panel);
                _highlightables.Add(panel);
            }

            protected virtual void HighlightElement(IHighlightable highlightable)
            {
                if (highlightable == null || ReferenceEquals(highlightable, _highlightedElement))
                {
                    return;
                }
                UnhighlightElement(_highlightedElement);
                highlightable.Highlight();
                _highlightedElement = highlightable;
            }

            protected virtual void UnhighlightElement(IHighlightable hightlightable)
            {
                if (hightlightable == null)
                {
                    return;
                }
                hightlightable.Unhighlight();

                if (_highlightedElement == hightlightable)
                {
                    _highlightedElement = null;
                }
            }

            protected virtual void ActivateHighlightable(IHighlightable highlightable)
            {
                if (highlightable == null) return;
                if (highlightable is IButton)
                {
                    ((IButton)highlightable).Press();
                }
                else if (highlightable is INavigable)
                {
                    NavigateElement((INavigable)highlightable);
                }
            }

            public virtual void NavigateElement(INavigable navigable)
            {
                if (navigable == null || ReferenceEquals(navigable, _navigatedElement) || !ReferenceEquals(this, navigable.Parent))
                {
                    return;
                }
                _navigatedElement?.PauseNavigation();

                _navigables.Add(navigable);
                _navigatedElement = navigable;

                navigable.StartNavigation(this);
                navigable.RequestStopNavigation += StopNavigatingElement;
            }

            public virtual void StopNavigatingElement(INavigable navigable)
            {
                if (navigable == null || !ReferenceEquals(this, navigable.Parent))
                {
                    return;
                }
                if (ReferenceEquals(navigable, _navigatedElement))
                {
                    _navigatedElement = null;
                }
                _navigables.Remove(navigable);
                navigable.StopNavigation(this);
                navigable.RequestStopNavigation -= StopNavigatingElement;

                NavigateElement(_navigables.LastOrDefault());
            }

            public virtual void OpenMenu(IMenu menu)
            {
                if (menu == null || !ReferenceEquals(this, menu.Parent)) return;
                _updatables.Add(menu);
                _uiElements.Add(menu);

                NavigateElement(menu);

                menu.Open(this);
                menu.RequestClose += CloseMenu;
            }

            public virtual void CloseMenu(IMenu menu)
            {
                if (menu == null || ReferenceEquals(this, menu.Parent)) return;
                _updatables.Remove(menu);
                _uiElements.Remove(menu);

                StopNavigatingElement(menu);

                menu.RequestClose -= CloseMenu;
                menu.Close(this);
            }

            public virtual void Update(DateTime time)
            {
                if (!IsOpen) return;
                Time = time;
                foreach (var updatable in _updatables.ToList())
                {
                    updatable.Update(time);
                }
            }

            public virtual void Draw(MySpriteDrawFrame frame)
            {
                if (!IsOpen) return;
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
            }

            public virtual void Navigate(UserInput input, object caller)
            {
                if (!IsOpen || !IsNavigating || IsPaused || !ReferenceEquals(Parent, caller))
                {
                    return;
                }
                if (_navigatedElement != null)
                {
                    _navigatedElement.Navigate(input, this);
                    return;
                }

                if (input.CRelease)
                {
                    Close();
                    return;
                }

                if (_highlightables.Count == 0)
                {
                    return;
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
            }
        }
    }
}
