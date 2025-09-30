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
            public Vector2 SurfaceSize => _display.SurfaceSize;
            public Vector2 TextureSize => _display.TextureSize;
            public UIWireManager UIWireManager { get; private set; }

            private IMyTextSurface _display;
            private IWindow _activeWindow = null;
            private IModal _activeModal = null;
            private int _runCounter;
            public UI (IController controller, IMyTextSurface display, UIWireManager uiWireManager)
            {
                Controller = controller;
                _display = display;
                UIWireManager = uiWireManager;

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;

                MainWindow mainWindow = new MainWindow(this);
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

            private void ExitCurrentWindow()
            {
                _activeWindow?.Exit();
                _activeWindow = null;
            }

            public void EnterModal(IModal modal)
            {
                _activeModal = modal;
            }

            private void CleanUp()
            {
                if (!_activeWindow?.IsInside ?? false)
                {
                    _activeWindow = null;
                }
                if (_activeModal?.CanClose ?? false)
                {
                    _activeModal = null;
                }
            }

            public void Update(DateTime time)
            {
                CleanUp();

                if (!HasActiveWindow)
                {
                    MainWindow mainWindow = new MainWindow(this);
                    EnterWindow(mainWindow);
                }

                if (_activeWindow is IUpdatable)
                {
                    ((IUpdatable)_activeWindow).Update(time);
                }
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (_activeModal != null)
                {
                    return;
                }
                _activeWindow?.Navigate(input, time);
            }

            public void Draw()
            {
                var frame = _display.DrawFrame();
                _activeWindow?.Draw(frame);
                _activeModal?.Draw(frame);
                frame.Dispose();
            }
        }
    }
}
