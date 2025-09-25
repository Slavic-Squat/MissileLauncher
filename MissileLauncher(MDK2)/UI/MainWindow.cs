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
            public Vector2 Pos { get; private set; }
            public Vector2 Size { get; private set; }
            private MySprite _sprite;
            private List<IMenu> _menus = new List<IMenu>();
            private IMenu _focusedMenu;


            public MainWindow(UI ui, Vector2 pos, Vector2 size)
            {
                UI = ui;
                Pos = pos;
                Size = size;
                OpenMenu(new MainMenu(this, pos, size - 10));

                _sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pos,
                    Size = size,
                    Color = UIConfig.WindowBackgroundColor,
                    Alignment = TextAlignment.CENTER
                };
            }

            public void RequestClose()
            {
                UI.CloseWindow();
            }

            public void OnClose()
            {
                _focusedMenu?.OnClose();
                UnfocusMenu();
                foreach (var menu in _menus)
                {
                    menu.OnClose();
                }
            }

            public void OpenMenu(IMenu menu)
            {
                _menus.Add(menu);
                FocusMenu(menu);
            }

            public void CloseMenu(IMenu menu)
            {
                if (_focusedMenu == menu)
                {
                    UnfocusMenu();
                }
                _menus.Remove(menu);
                menu.OnClose();
            }

            public void FocusMenu(IMenu menu)
            {
                if (_focusedMenu != null)
                {
                    UnfocusMenu();
                }
                _focusedMenu = menu;
                _focusedMenu.OnFocus();
            }

            public void UnfocusMenu()
            {
                _focusedMenu?.OnUnfocus();
                _focusedMenu = null;
            }

            public void HighlightMenu(IMenu menu)
            {

            }

            public void UnhighlightMenu()
            {

            }

            public void Update(DateTime time)
            {
                foreach (var menu in _menus)
                {
                    menu.Update(time);
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.Add(_sprite);
                foreach (var menu in Enumerable.Reverse(_menus))
                {
                    menu.Draw(frame);
                }
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (_focusedMenu != null)
                {
                    _focusedMenu.Navigate(input, time);
                }
                else if (input.CHeldAndReleased)
                {
                    RequestClose();
                }
            }
        }
    }
}
