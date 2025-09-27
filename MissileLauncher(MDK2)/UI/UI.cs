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
        public class UI : INavigable
        {
            public IController Controller { get; private set; }
            public bool HasActiveWindow => _activeWindow != null;

            private UIWireManager _uiWireManager;
            private IMyTextSurface _display;
            private IWindow _activeWindow = null;
            private int _runCounter;
            public UI (IController controller, IMyTextSurface display, UIWireManager uiWireManager)
            {
                Controller = controller;
                _display = display;
                _uiWireManager = uiWireManager;

                _display.ContentType = ContentType.SCRIPT;
                _display.Script = "";
                _display.ScriptBackgroundColor = Color.Black;

                MainWindow mainWindow = new MainWindow(this, new Vector2(_display.TextureSize.X / 2f, _display.TextureSize.Y / 2f), new Vector2(_display.SurfaceSize.X, _display.SurfaceSize.Y), _uiWireManager);
                EnterWindow(mainWindow);
            }

            public void Run(DateTime time)
            {
                if (_runCounter++ >= 9)
                {
                    Update(time);
                    Draw();
                    _runCounter = 0;
                }
            }

            public void EnterWindow(IWindow window)
            {
                ExitCurrentWindow();
                _activeWindow = window;
                _activeWindow.Enter();
            }

            public void ExitCurrentWindow()
            {
                _activeWindow?.Exit();
                _activeWindow = null;
            }

            public void Update(DateTime time)
            {
                if (!HasActiveWindow)
                {
                    MainWindow mainWindow = new MainWindow(this, new Vector2(_display.TextureSize.X / 2f, _display.TextureSize.Y / 2f), new Vector2(_display.SurfaceSize.X, _display.SurfaceSize.Y), _uiWireManager);
                    EnterWindow(mainWindow);
                }
                _activeWindow?.Update(time);
            }

            public void Navigate(UserInput input, DateTime time)
            {
                _activeWindow?.Navigate(input, time);
            }

            public void Draw()
            {
                var frame = _display.DrawFrame();
                _activeWindow?.Draw(frame);
                frame.Dispose();
            }
        }
    }
}
