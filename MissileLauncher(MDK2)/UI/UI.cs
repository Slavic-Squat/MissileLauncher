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
        public class UI
        {
            private IMyTextSurface _display;
            private IWindow _currentWindow = null;
            public UI (Program program, IMyTextSurface display)
            {
                _display = display;
                _display.ContentType = ContentType.SCRIPT;
                _display.Script = "";
                _display.ScriptBackgroundColor = Color.Black;

                MainWindow mainWindow = new MainWindow(this, new Vector2(_display.TextureSize.X / 2f, _display.TextureSize.Y / 2f), new Vector2(_display.SurfaceSize.X, _display.SurfaceSize.Y));
                OpenWindow(mainWindow);
            }

            public void Run(DateTime time, UserInput input)
            {
                Navigate(input, time);
                Update(time);
                Draw();
            }

            public void OpenWindow(IWindow window)
            {
                CloseWindow();
                _currentWindow = window;
            }

            public void CloseWindow()
            {
                _currentWindow?.OnClose();
                _currentWindow = null;
            }

            public void Update(DateTime time)
            {
                if (_currentWindow == null)
                {
                    MainWindow mainWindow = new MainWindow(this, new Vector2(_display.TextureSize.X / 2f, _display.TextureSize.Y / 2f), new Vector2(_display.SurfaceSize.X, _display.SurfaceSize.Y));
                    OpenWindow(mainWindow);
                }
                _currentWindow?.Update(time);
            }

            public void Navigate(UserInput input, DateTime time)
            {
                _currentWindow?.Navigate(input, time);
            }

            public void Draw()
            {
                var frame = _display.DrawFrame();
                _currentWindow?.Draw(frame);
                frame.Dispose();
            }
        }
    }
}
