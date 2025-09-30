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
        public class LaserWindow : IWindow, IUpdatable
        {
            public UI UI { get; private set; }
            public Vector2 Pos { get; private set; }
            public Vector2 Size { get; private set; }
            public bool IsInside { get; private set; }

            private List<TargetingLaser> _lasers;

            private List<MySprite> _sprites = new List<MySprite>();
            private List<IHighlightable> _highlightableElements = new List<IHighlightable>();
            private List<IUpdatable> _updatableElements = new List<IUpdatable>();
            private List<IUIElement> _allElements = new List<IUIElement>();
            private IHighlightable _highlightedElement;
            private IEnterable _enteredElement;
            private int _pageIndex = 0;


            public LaserWindow(UI ui, Vector2 pos, Vector2 size)
            {
                UI = ui;
                Pos = pos;
                Size = size;

                Init();
            }

            public LaserWindow(UI ui)
            {
                UI = ui;
                Pos = new Vector2(ui.TextureSize.X / 2f, ui.TextureSize.Y / 2f);
                Size = new Vector2(ui.SurfaceSize.X, ui.SurfaceSize.Y);

                _lasers = UI.UIWireManager.GetTargetingLasers();

                Init();
            }

            public void Init()
            {
                BuildSprites();

                for (int i = 0; i < _lasers.Count; i++)
                {
                    TargetingLaser laser = _lasers[i];
                    Button button = new Button($"Laser [{i}]", Pos + new Vector2(i % 2 * 320 - Size.X / 2 + 160, i / 2 * 140 - Size.Y / 2 + 180), new Vector2(240, 80), $"Laser [{i}]", 1.2f, () =>
                    {
                        UI.Controller.TakeControl(laser);
                        return true;
                    });

                    _highlightableElements.Add(button);
                    _updatableElements.Add(button);
                    _allElements.Add(button);
                }
            }

            private void BuildSprites()
            {
                _sprites.Clear();
                MySprite fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos,
                    Size = Size,
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(fillSprite);

                MySprite headerBorderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos + new Vector2(0, -Size.Y / 2 + 30),
                    Size = new Vector2(460, 80),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(headerBorderSprite);

                MySprite headerFillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos + new Vector2(0, -Size.Y / 2 + 30),
                    Size = new Vector2(440, 60),
                    Color = new Color(32, 32, 32, 255),
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(headerFillSprite);

                MySprite headerTextSprite = SpriteHelper.CreateText(Pos + new Vector2(0, -Size.Y / 2 + 30), "Select Laser To Control:", Color.White, 1.5f);
                _sprites.Add(headerTextSprite);
            }

            public void Enter()
            {
                if (_highlightableElements.Count > 0)
                {
                    HighlightElement(_highlightableElements[0]);
                }
                IsInside = true;
            }

            public void Exit()
            {
                IsInside = false;
                UnhighlightCurrentElement();
                ExitCurrentElement();
            }

            private void HighlightElement(IHighlightable highlightable)
            {
                UnhighlightCurrentElement();
                highlightable.Highlight();
                _highlightedElement = highlightable;
            }

            private void UnhighlightCurrentElement()
            {
                _highlightedElement?.Unhighlight();
                _highlightedElement = null;
            }

            private void ActivateHighlightedElement(DateTime time)
            {
                if (_highlightedElement is IButton)
                {
                    ((IButton)_highlightedElement).Press(time);
                }
                else if (_highlightedElement is IEnterable)
                {
                    EnterElement((IEnterable)_highlightedElement);
                }
            }

            private void EnterElement(IEnterable enterable)
            {
                ExitCurrentElement();
                enterable.Enter();
                _enteredElement = enterable;
            }

            private void ExitCurrentElement()
            {
                if (_enteredElement != null)
                {
                    _enteredElement.Exit();
                    _enteredElement = null;
                }
            }

            private void CleanUp()
            {
                if (!_enteredElement?.IsInside ?? false)
                {
                    _enteredElement = null;
                }
            }

            public void Update(DateTime time)
            {
                CleanUp();

                foreach (var element in _updatableElements)
                {
                    if (element == _enteredElement)
                    {
                        continue;
                    }
                    element.Update(time);
                }

                if (_enteredElement is IUpdatable)
                {
                    ((IUpdatable)_enteredElement).Update(time);
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.AddRange(_sprites);

                foreach (var element in _allElements)
                {
                    if (element == _enteredElement || element == _highlightedElement)
                    {
                        continue;
                    }

                    element.Draw(frame);
                }
                _highlightedElement?.Draw(frame);
                _enteredElement?.Draw(frame);
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (_enteredElement is INavigable)
                {
                    ((INavigable)_enteredElement).Navigate(input, time);
                }
                if (_enteredElement != null)
                {
                    return;
                }

                if (_highlightableElements.Count == 0)
                {
                    return;
                }

                if (input.CRelease)
                {
                    Exit();
                }
                else if (input.WRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightableElements, _highlightedElement, UIUtilities.NavigationDirection.Up);
                    HighlightElement(nextElement);
                }
                else if (input.SRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightableElements, _highlightedElement, UIUtilities.NavigationDirection.Down);
                    HighlightElement(nextElement);
                }
                else if (input.ARelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightableElements, _highlightedElement, UIUtilities.NavigationDirection.Left);
                    HighlightElement(nextElement);
                }
                else if (input.DRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightableElements, _highlightedElement, UIUtilities.NavigationDirection.Right);
                    HighlightElement(nextElement);
                }
                else if (input.SpaceRelease)
                {
                    ActivateHighlightedElement(time);
                }
            }
        }
    }
}
