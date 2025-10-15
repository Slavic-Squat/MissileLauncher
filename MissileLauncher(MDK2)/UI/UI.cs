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
            public ControlStation Station { get; private set; }
            public double Time { get; private set; }
            public bool HasActiveWindow => _activeWindow != null;
            public IMyTextSurface Display { get; private set; }
            public Vector2 SurfaceSize => Display.SurfaceSize;
            public Vector2 TextureSize => Display.TextureSize;
            public UIWireManager UIWireManager { get; private set; }
            public RectangleF Bounds { get; private set; }
            public Vector2 Center => Bounds.Center;
            public Vector2 Size => Bounds.Size;
            public Vector2 Pos => Bounds.Position;

            private IWindow _activeWindow = null;
            private IModal _activeModal = null;
            private int _runCounter;
            public UI (ControlStation station, IMyTextSurface display, UIWireManager uiWireManager)
            {
                Station = station;
                Display = display;
                UIWireManager = uiWireManager;

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;

                Bounds = new RectangleF(new Vector2(0, (TextureSize.Y - SurfaceSize.Y) * 0.5f), SurfaceSize);

                Window mainWindow = UIFactory.CreateMainWindow(this, 5f);
                OpenWindow(mainWindow);
            }

            public void Run(double time)
            {
                Time = time;
                Update(time);
                if (_runCounter++ >= 9)
                {
                    Draw();
                    _runCounter = 0;
                }
            }

            public bool OpenWindow(IWindow window)
            {
                if (window == null || ReferenceEquals(_activeWindow, window) || !ReferenceEquals(this, window.Parent))
                {
                    return false;
                }
                CloseWindow(_activeWindow);
                _activeWindow = window;
                window.Open(this);
                window.StartNavigation(this);
                window.RequestClose += CloseWindow;
                return true;
            }

            public bool CloseWindow(IWindow window)
            {
                if (window == null || !ReferenceEquals(this, window.Parent))
                {
                    return false;
                }
                if (ReferenceEquals(_activeWindow, window))
                {
                    _activeWindow = null;
                }
                window.Close(this);
                window.StopNavigation(this);
                window.RequestClose -= CloseWindow;
                return true;
            }

            public bool OpenModal(IModal modal)
            {
                if (modal == null || ReferenceEquals(_activeModal, modal))
                {
                    return false;
                }
                _activeModal = modal;
                return true;
            }

            public bool Update(double time)
            {
                if (_activeModal?.CanClose ?? false)
                {
                    _activeModal = null;
                }

                if (!HasActiveWindow)
                {
                    Window mainWindow = UIFactory.CreateMainWindow(this, 5f);
                    OpenWindow(mainWindow);
                }

                _activeWindow?.Update(time);
                return true;
            }

            public bool Navigate(UserInput input, object caller)
            {
                if (!ReferenceEquals(Station, caller))
                {
                    return false;
                }
                if (_activeModal != null)
                {
                    return true;
                }
                _activeWindow?.Navigate(input, this);
                return true;
            }

            public bool Draw()
            {
                var frame = Display.DrawFrame();
                _activeWindow?.Draw(frame);
                _activeModal?.Draw(frame);
                frame.Dispose();
                return true;
            }
        }
    }
}
