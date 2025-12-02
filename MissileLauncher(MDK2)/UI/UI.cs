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
            public UICoordinator UICoordinator { get; private set; }
            public RectangleF Bounds { get; private set; }
            public Vector2 Center => Bounds.Center;
            public Vector2 Size => Bounds.Size;
            public Vector2 Pos => Bounds.Position;

            private IWindow _activeWindow = null;
            private IPopUp _activePopUp = null;
            public UI (ControlStation station, IMyTextSurface display, UICoordinator uiCoordinator)
            {
                Station = station;
                Display = display;
                UICoordinator = uiCoordinator;

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;

                Bounds = new RectangleF(new Vector2(0, (TextureSize.Y - SurfaceSize.Y) * 0.5f), SurfaceSize);

                TargetingWindow targetingWindow = new TargetingWindow(this, 5f);
                OpenWindow(targetingWindow);
            }

            public void Run(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }

                Update(time);
                Draw();
                Time = time;
            }

            public void OpenWindow(IWindow window)
            {
                if (window == null || ReferenceEquals(_activeWindow, window) || !ReferenceEquals(this, window.Parent))
                {
                    return;
                }
                CloseWindow(_activeWindow);
                _activeWindow = window;
                window.Open(this);
                window.StartNavigation(this);
                window.RequestClose += CloseWindow;
            }

            public void CloseWindow(IWindow window)
            {
                if (window == null || !ReferenceEquals(this, window.Parent))
                {
                    return;
                }
                if (ReferenceEquals(_activeWindow, window))
                {
                    _activeWindow = null;
                }
                window.Close(this);
                window.StopNavigation(this);
                window.RequestClose -= CloseWindow;
            }

            public void OpenPopUp(IPopUp popUp)
            {
                if (popUp == null || ReferenceEquals(_activePopUp, popUp))
                {
                    return;
                }
                _activePopUp = popUp;
            }

            public void Update(double time)
            {
                if (_activePopUp?.CanClose ?? false)
                {
                    _activePopUp = null;
                }

                if (!HasActiveWindow)
                {
                    TargetingWindow targetingWindow = new TargetingWindow(this, 5f);
                    OpenWindow(targetingWindow);
                }

                _activeWindow?.Update(time);
            }

            public void Navigate(UserInput input, object caller)
            {
                if (!ReferenceEquals(Station, caller))
                {
                    return;
                }
                if (_activePopUp != null)
                {
                    return;
                }
                _activeWindow?.Navigate(input, this);
            }

            public void Draw()
            {
                var frame = Display.DrawFrame();
                _activeWindow?.Draw(frame);
                _activePopUp?.Draw(frame);
                frame.Dispose();
            }
        }
    }
}
