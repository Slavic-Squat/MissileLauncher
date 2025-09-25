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
        public class MainMenu : IMenu
        {
            public IWindow Window { get; private set; }
            public Vector2 Pos { get; private set; }
            public Vector2 Size { get; private set; }
            private MySprite _sprite;
            private Dictionary<string, ButtonElement> _buttons = new Dictionary<string, ButtonElement>();
            private ButtonElement _highlightedButton = null;
            public MainMenu(IWindow window, Vector2 pos, Vector2 size)
            {
                Window = window;
                Pos = pos;
                Size = size;
                _sprite = new MySprite()
               {
                   Type = SpriteType.TEXTURE,
                   Data = "SquareSimple",
                   Position = pos,
                   Size = size,
                   Color = UIConfig.MenuBackgroundColor,
                    Alignment = TextAlignment.CENTER
               };

                _buttons.Add("RADAR", new ButtonElement("RADAR", pos + new Vector2(-250, 0), new Vector2(400, 100), "RADAR", 2.0f, () => {
                    RadarWindow radarWindow = new RadarWindow(Window.UI, Window.Pos, Window.Size);
                    Window.UI.OpenWindow(radarWindow);
                }));
                _buttons.Add("LASER CTRL", new ButtonElement("LASER CTRL", pos + new Vector2(250, 0), new Vector2(400, 100), "LASER CTRL", 2.0f, () => { }));
            }

            public void RequestClose()
            {
                Window.CloseMenu(this);
            }

            public void OnClose()
            {
                UnhighlightButton();
            }

            public void OnFocus()
            {
                HighlightButton(_buttons.Values.OrderBy(x => x.Pos.X).ThenBy(x => x.Pos.Y).FirstOrDefault());
            }

            public void OnUnfocus()
            {
                UnhighlightButton();
            }

            public void HighlightButton(ButtonElement button)
            {
                if (_highlightedButton != null)
                {
                    UnhighlightButton();
                }
                button.OnHighlight();
                _highlightedButton = button;
            }

            public void UnhighlightButton()
            {
                _highlightedButton?.OnUnhighlight();
                _highlightedButton = null;
            }

            public void PressButton(ButtonElement button, DateTime time)
            {
                button.Press(time);
            }

            public void Update(DateTime time)
            {
                foreach (var button in _buttons.Values)
                {
                    button.Update(time);
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.Add(_sprite);
                foreach (var button in _buttons.Values)
                {
                    button.Draw(frame);
                }
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (input.WRelease)
                {
                    ButtonElement nextButton = _buttons.Values.Where(button => button.Pos.Y < _highlightedButton.Pos.Y).OrderBy(button =>
                    {
                        float dx = Math.Abs(button.Pos.X - _highlightedButton.Pos.X);
                        float dy = Math.Abs(button.Pos.Y - _highlightedButton.Pos.Y);
                        return dx * 10 + dy;
                    }).FirstOrDefault() ?? _highlightedButton;

                    HighlightButton(nextButton);
                }
                else if (input.SRelease)
                {
                    ButtonElement nextButton = _buttons.Values.Where(button => button.Pos.Y > _highlightedButton.Pos.Y).OrderBy(button =>
                    {
                        float dx = Math.Abs(button.Pos.X - _highlightedButton.Pos.X);
                        float dy = Math.Abs(button.Pos.Y - _highlightedButton.Pos.Y);
                        return dx * 10 + dy;
                    }).FirstOrDefault() ?? _highlightedButton;

                    HighlightButton(nextButton);
                }
                else if (input.ARelease)
                {
                    ButtonElement nextButton = _buttons.Values.Where(button => button.Pos.X < _highlightedButton.Pos.X).OrderBy(button =>
                    {
                        float dx = Math.Abs(button.Pos.X - _highlightedButton.Pos.X);
                        float dy = Math.Abs(button.Pos.Y - _highlightedButton.Pos.Y);
                        return dx + dy * 10;
                    }).FirstOrDefault() ?? _highlightedButton;

                    HighlightButton(nextButton);
                }
                else if (input.DRelease)
                {
                    ButtonElement nextButton = _buttons.Values.Where(button => button.Pos.X > _highlightedButton.Pos.X).OrderBy(button =>
                    {
                        float dx = Math.Abs(button.Pos.X - _highlightedButton.Pos.X);
                        float dy = Math.Abs(button.Pos.Y - _highlightedButton.Pos.Y);
                        return dx + dy * 10;
                    }).FirstOrDefault() ?? _highlightedButton;

                    HighlightButton(nextButton);
                }

                if (input.SpaceRelease)
                {
                    PressButton(_highlightedButton, time);
                }
                if (input.CHeldAndReleased)
                {
                    RequestClose();
                }
            }
        }
    }
}
