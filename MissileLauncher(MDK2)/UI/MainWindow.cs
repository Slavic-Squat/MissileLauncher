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
        public class MainWindow : IWindow
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

            private RectangleF _bounds;
            private float _borderThickness;
            private List<MySprite> _sprites = new List<MySprite>();
            private List<IButton> _buttons = new List<IButton>();
            private IButton _highlightedButton;


            public MainWindow(UI ui, Vector2 pos, Vector2 size, float borderThickness)
            {
                UI = ui;

                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;

                Init();
            }

            public MainWindow(UI ui, float borderThickness)
            {
                UI = ui;
                Vector2 pos = (ui.TextureSize - ui.SurfaceSize) * 0.5f;
                Vector2 size = new Vector2(ui.SurfaceSize.X, ui.SurfaceSize.Y);

                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;

                Init();
            }

            private void Init()
            {
                BuildSprites();

                Vector2 laserButtonSize = new Vector2(400, 100);
                Vector2 laserButtonPos = Pos + new Vector2(50, Size.Y * 0.5f - laserButtonSize.Y * 0.5f);
                
                Button laserButton = new Button("LASER CTRL", laserButtonPos, laserButtonSize, 15f, 10f, 5f, () => "LASER CTRL", () =>
                {
                    UI.OpenWindow(new LaserWindow(UI, 10f));
                    return true;
                },
                Display);

                Vector2 radarButtonSize = new Vector2(400, 100);
                Vector2 radarButtonPos = Pos + new Vector2(Bounds.Right - radarButtonSize.X - 50, Size.Y * 0.5f - radarButtonSize.Y * 0.5f);
                
                Button radarButton = new Button("RADAR", radarButtonPos, radarButtonSize, 15f, 10f, 5f, () => "RADAR", () => 
                { 
                    UI.OpenWindow(new RadarWindow(UI, 10f));
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
                if (button == null) return;
                button.Unhighlight();
                if (button == _highlightedButton)
                {
                    _highlightedButton = null;
                }
            }

            private void ActivateButton(IButton button, DateTime time)
            {
                if (button == null) return;
                button.Press(time);
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
