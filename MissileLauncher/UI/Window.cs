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

            protected double _time;

            protected bool _canUserClose;

            protected RectangleF _bounds;
            protected float _borderThickness;

            protected List<MySprite> _bodySprites = new List<MySprite>();
            protected List<MySprite> _additionalSprites = new List<MySprite>();

            protected List<IHighlightable> _highlightables = new List<IHighlightable>();
            protected IHighlightable _highlightedElement;
            protected List<IUpdatable> _updatables = new List<IUpdatable>();
            protected List<IUIElement> _uiElements = new List<IUIElement>();
            protected List<INavigable> _navigables = new List<INavigable>();
            protected INavigable _navigatedElement;


            public Window(UI ui, Vector2 pos, Vector2 size, float borderThickness, bool canUserClose = true)
            {
                UI = ui;
                Parent = ui;

                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _canUserClose = canUserClose;

                Init();
            }

            public Window(UI ui, float borderThickness, bool canUserClose = true)
            {
                UI = ui;
                Parent = ui;

                _bounds = ui.Bounds;
                _borderThickness = borderThickness;
                _canUserClose = canUserClose;

                Init();
            }

            private void Init()
            {

            }

            protected virtual void BuildSprites()
            {
                _bodySprites.Clear();
                SpriteHelper.CreateBoxFilled(_bodySprites, Bounds, UIConfig.WindowBorderColor, UIConfig.WindowFillColor, _borderThickness);
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

            public virtual void ResumeNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsPaused = false;
                _navigatedElement?.ResumeNavigation(this);
            }

            public virtual void PauseNavigation(object caller)
            {
                if (!ReferenceEquals(Parent, caller))
                {
                    return;
                }
                IsPaused = true;
                UnhighlightElement(_highlightedElement);
                _navigatedElement?.PauseNavigation(this);
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

            protected virtual void UnhighlightElement(IHighlightable highlightable)
            {
                if (highlightable == null)
                {
                    return;
                }
                highlightable.Unhighlight();

                if (_highlightedElement == highlightable)
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
                    StartNavigatingElement((INavigable)highlightable);
                }
            }

            public virtual void StartNavigatingElement(INavigable navigable)
            {
                if (navigable == null || ReferenceEquals(navigable, _navigatedElement) || !ReferenceEquals(this, navigable.Parent))
                {
                    return;
                }
                _navigables.Add(navigable);
                navigable.StartNavigation(this);
                navigable.RequestStopNavigation += StopNavigatingElement;
                
                _navigatedElement?.PauseNavigation(this);
                _navigatedElement = navigable;
            }

            public virtual void StopNavigatingElement(INavigable navigable)
            {
                if (navigable == null || !ReferenceEquals(this, navigable.Parent))
                {
                    return;
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
                        _navigatedElement.ResumeNavigation(this);
                    }
                }
            }

            public virtual void OpenMenu(IMenu menu)
            {
                if (menu == null || !ReferenceEquals(this, menu.Parent)) return;
                _updatables.Add(menu);
                _uiElements.Add(menu);

                menu.Open(this);
                menu.RequestClose += CloseMenu;

                StartNavigatingElement(menu);
            }

            public virtual void CloseMenu(IMenu menu)
            {
                if (menu == null || !ReferenceEquals(this, menu.Parent)) return;
                _updatables.Remove(menu);
                _uiElements.Remove(menu);

                StopNavigatingElement(menu);

                menu.RequestClose -= CloseMenu;
                menu.Close(this);
            }

            public virtual void Update(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                if (!IsOpen) return;
                foreach (var updatable in _updatables.ToList())
                {
                    updatable.Update(time);
                }
                _time = time;
            }

            public virtual void Draw(MySpriteDrawFrame frame)
            {
                if (!IsOpen) return;
                BuildSprites();
                frame.AddRange(_bodySprites);

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
