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
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsInside { get; private set; }

            public IMyTextSurface Display => UI.Display;

            private List<TargetingLaser> Lasers => UI.UIWireManager.TargetingLasers;

            private RectangleF _bounds;
            private List<MySprite> _sprites = new List<MySprite>();
            private List<IButton> _buttons = new List<IButton>();
            private IButton _highlightedButton;
            private IEnterable _enteredElement;


            public LaserWindow(UI ui, Vector2 pos, Vector2 size)
            {
                UI = ui;

                _bounds = new RectangleF(pos, size);

                Init();
            }

            public LaserWindow(UI ui)
            {
                UI = ui;

                Vector2 pos = (ui.TextureSize - ui.SurfaceSize) * 0.5f;
                Vector2 size = new Vector2(ui.SurfaceSize.X, ui.SurfaceSize.Y);

                _bounds = new RectangleF(pos, size);

                Init();
            }

            public void Init()
            {
                BuildSprites();

                for (int i = 0; i < Lasers.Count; i++)
                {
                    TargetingLaser laser = Lasers[i];
                    Vector2 size = new Vector2(240, 80);
                    Vector2 pos = Pos + new Vector2(i % 2 * (size.X + 50) + 50, i / 2 * (size.Y + 50) + 50);
                    
                    Button button = new Button($"Laser [{i}]", pos, size, $"Laser [{i}]", () =>
                    {
                        UI.Controller.TakeControl(laser);
                        return true;
                    },
                    Display);

                    _buttons.Add(button);
                }
            }

            private void BuildSprites()
            {
                _sprites.Clear();
                MySprite fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(fillSprite);

                Vector2 headerSize = new Vector2(440, 60);
                Vector2 headerPos = Pos + new Vector2(Center.X - headerSize.X * 0.5f, 0);
                RectangleF headerBounds = new RectangleF(headerPos, headerSize);

                float headerBorderThickness = 20f;

                MySprite headerBorderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = headerBounds.Center,
                    Size = headerBounds.Size,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(headerBorderSprite);

                MySprite headerFillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = headerBounds.Center,
                    Size = headerBounds.Size - headerBorderThickness,
                    Color = new Color(32, 32, 32, 255),
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(headerFillSprite);

                MySprite headerTextSprite = SpriteHelper.CreateText(headerBounds, "Select Laser To Control:", Color.White, Display, TextAlignment.CENTER, true, 0.75f);
                _sprites.Add(headerTextSprite);
            }

            public void Enter()
            {
                if (_buttons.Count > 0)
                {
                    HighlightButton(_buttons[0]);
                }
                IsInside = true;
            }

            public void Exit()
            {
                IsInside = false;
                UnhighlightCurrentButton();
                ExitCurrentElement();
            }

            private void HighlightButton(IButton button)
            {
                UnhighlightCurrentButton();
                button.Highlight();
                _highlightedButton = button;
            }

            private void UnhighlightCurrentButton()
            {
                _highlightedButton?.Unhighlight();
                _highlightedButton = null;
            }

            private void ActivateHighlightedButton(DateTime time)
            {
                _highlightedButton?.Press(time);
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

                foreach (var button in _buttons)
                {
                    button.Update(time);
                }

                if (_enteredElement is IUpdatable)
                {
                    ((IUpdatable)_enteredElement).Update(time);
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.AddRange(_sprites);

                foreach (var button in _buttons)
                {
                    if (button == _highlightedButton)
                    {
                        continue;
                    }

                    button.Draw(frame);
                }
                _highlightedButton?.Draw(frame);
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

                if (input.CRelease)
                {
                    Exit();
                }

                if (_buttons.Count == 0)
                {
                    return;
                }

                if (input.WRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, UIUtilities.NavigationDirection.Up);
                    HighlightButton(nextButton);
                }
                else if (input.SRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, UIUtilities.NavigationDirection.Down);
                    HighlightButton(nextButton);
                }
                else if (input.ARelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, UIUtilities.NavigationDirection.Left);
                    HighlightButton(nextButton);
                }
                else if (input.DRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, UIUtilities.NavigationDirection.Right);
                    HighlightButton(nextButton);
                }
                else if (input.SpaceRelease)
                {
                    ActivateHighlightedButton(time);
                }
            }
        }
    }
}
