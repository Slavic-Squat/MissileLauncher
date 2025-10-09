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
        public class LaserWindow : IWindow
        {
            public UI UI { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsOpen { get; private set; }
            public bool IsNavigating { get; private set; }
            public bool IsPaused { get; private set; }
            public event Action<IWindow> RequestClose;
            public event Action<INavigable> RequestStopNavigation;

            public IMyTextSurface Display => UI.Display;

            private List<TargetingLaser> Lasers => UI.UIWireManager.TargetingLasers;

            private RectangleF _bounds;
            private float _borderThickness;
            private List<MySprite> _sprites = new List<MySprite>();
            private List<IButton> _buttons = new List<IButton>();
            private IButton _highlightedButton;


            public LaserWindow(UI ui, Vector2 pos, Vector2 size, float borderThickness)
            {
                UI = ui;

                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;

                Init();
            }

            public LaserWindow(UI ui, float borderThickness)
            {
                UI = ui;

                Vector2 pos = (ui.TextureSize - ui.SurfaceSize) * 0.5f;
                Vector2 size = new Vector2(ui.SurfaceSize.X, ui.SurfaceSize.Y);

                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;

                Init();
            }

            public void Init()
            {
                BuildSprites();

                for (int i = 0; i < Lasers.Count; i++)
                {
                    TargetingLaser laser = Lasers[i];
                    Vector2 size = new Vector2(240, 80);
                    Vector2 pos = Pos + new Vector2(i % 2 * (size.X + 50) + 50, i / 2 * (size.Y + 50) + 100);
                    
                    Button button = new Button($"Laser [{i}]", pos, size, 12f, 8f, 4f, () => $"Laser [{i}]", () =>
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
                    Size = headerBounds.Size - 10f,
                    Color = new Color(32, 32, 32, 255),
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(headerFillSprite);

                MySprite headerTextSprite = SpriteHelper.CreateText(headerBounds, "Select Laser To Control:", Color.White, Display, TextAlignment.CENTER, true, 10f);
                _sprites.Add(headerTextSprite);
            }

            public void OnOpen()
            {
                IsOpen = true;
            }

            private void Close()
            {
                RequestClose?.Invoke(this);
            }

            public void OnClose()
            {
                IsOpen = false;
            }

            public void OnStartNavigation()
            {
                IsNavigating = true;
                ResumeNavigation();
            }

            private void StopNavigation()
            {
                RequestStopNavigation?.Invoke(this);
            }

            public void OnStopNavigation()
            {
                IsNavigating = false;
                PauseNavigation();
            }

            public void ResumeNavigation()
            {
                IsPaused = false;
                if (_buttons.Count > 0)
                {
                    HighlightButton(_buttons[0]);
                }
            }

            public void PauseNavigation()
            {
                IsPaused = true;
                UnhighlightButton(_highlightedButton);
            }

            private void HighlightButton(IButton button)
            {
                UnhighlightButton(_highlightedButton);
                button.Highlight();
                _highlightedButton = button;
            }

            private void UnhighlightButton(IButton button)
            {
                button?.Unhighlight();
                if (_highlightedButton == button)
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
                foreach (var button in _buttons)
                {
                    button.Update(time);
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
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (input.CRelease)
                {
                    Close();
                }

                if (_buttons.Count == 0)
                {
                    return;
                }

                if (input.WRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, Direction.Up);
                    HighlightButton(nextButton);
                }
                else if (input.SRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, Direction.Down);
                    HighlightButton(nextButton);
                }
                else if (input.ARelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, Direction.Left);
                    HighlightButton(nextButton);
                }
                else if (input.DRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, Direction.Right);
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
