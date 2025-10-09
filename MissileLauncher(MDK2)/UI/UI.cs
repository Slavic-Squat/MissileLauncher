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
            public IController Controller { get; private set; }
            public bool HasActiveWindow => _activeWindow != null;
            public IMyTextSurface Display { get; private set; }
            public Vector2 SurfaceSize => Display.SurfaceSize;
            public Vector2 TextureSize => Display.TextureSize;
            public UIWireManager UIWireManager { get; private set; }

            private IWindow _activeWindow = null;
            private IModal _activeModal = null;
            private int _runCounter;
            public UI (IController controller, IMyTextSurface display, UIWireManager uiWireManager)
            {
                Controller = controller;
                Display = display;
                UIWireManager = uiWireManager;

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;

                MainWindow mainWindow = new MainWindow(this, 10f);
                OpenWindow(mainWindow);
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

            public void OpenWindow(IWindow window)
            {
                if (window == null || ReferenceEquals(_activeWindow, window))
                {
                    return;
                }
                CloseWindow(_activeWindow);
                _activeWindow = window;
                window.OnOpen();
                window.OnStartNavigation();
                window.RequestClose += CloseWindow;
            }

            public void CloseWindow(IWindow window)
            {
                if (window == null)
                {
                    return;
                }
                if (ReferenceEquals(_activeWindow, window))
                {
                    _activeWindow = null;
                }
                window.OnClose();
                window.OnStopNavigation();
                window.RequestClose -= CloseWindow;
            }

            public void OpenModal(IModal modal)
            {
                if (modal == null || ReferenceEquals(_activeModal, modal))
                {
                    return;
                }
                _activeModal = modal;
            }

            public void Update(DateTime time)
            {
                if (_activeModal?.CanClose ?? false)
                {
                    _activeModal = null;
                }

                if (!HasActiveWindow)
                {
                    MainWindow mainWindow = new MainWindow(this, 10f);
                    OpenWindow(mainWindow);
                }

                _activeWindow?.Update(time);
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
                var frame = Display.DrawFrame();
                _activeWindow?.Draw(frame);
                _activeModal?.Draw(frame);
                frame.Dispose();
            }
        }
    }
}
