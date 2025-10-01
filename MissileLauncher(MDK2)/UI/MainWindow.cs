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
        public class MainWindow : IWindow, IUpdatable
        {
            public UI UI { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsInside { get; private set; }

            public IMyTextSurface Display => UI.Display;

            private RectangleF _bounds;
            private List<MySprite> _sprites = new List<MySprite>();
            private List<IButton> _buttons = new List<IButton>();
            private IButton _highlightedButton;
            private IEnterable _enteredElement;


            public MainWindow(UI ui, Vector2 pos, Vector2 size)
            {
                UI = ui;

                _bounds = new RectangleF(pos, size);

                Init();
            }

            public MainWindow(UI ui)
            {
                UI = ui;
                Vector2 pos = (ui.TextureSize - ui.SurfaceSize) * 0.5f;
                Vector2 size = new Vector2(ui.SurfaceSize.X, ui.SurfaceSize.Y);

                _bounds = new RectangleF(pos, size);

                Init();
            }

            private void Init()
            {
                BuildSprites();

                Vector2 laserButtonSize = new Vector2(400, 100);
                Vector2 laserButtonPos = Pos + new Vector2(50, Size.Y * 0.5f - laserButtonSize.Y * 0.5f);
                
                Button laserButton = new Button("LASER CTRL", laserButtonPos, laserButtonSize, "LASER CTRL", () =>
                {
                    UI.EnterWindow(new LaserWindow(UI));
                    return true;
                },
                Display);

                Vector2 radarButtonSize = new Vector2(400, 100);
                Vector2 radarButtonPos = Pos + new Vector2(Bounds.Right - radarButtonSize.X - 50, Size.Y * 0.5f - radarButtonSize.Y * 0.5f);
                
                Button radarButton = new Button("RADAR", radarButtonPos, radarButtonSize, "RADAR", () => 
                { 
                    UI.EnterWindow(new RadarWindow(UI));
                    return true; 
                },
                Display);

                _buttons.Add(laserButton);
                _buttons.Add(radarButton);
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
                    Color = UIConfig.WindowFillColor,
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(fillSprite);
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
