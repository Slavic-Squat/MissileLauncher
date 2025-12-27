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
            private bool _isPaused = false;

            private int _runCounter = 0;
            public UI (ControlStation station, IMyTextSurface display, UICoordinator uiCoordinator)
            {
                Station = station;
                Display = display;
                UICoordinator = uiCoordinator;

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;

                Bounds = new RectangleF((TextureSize - SurfaceSize) * 0.5f, SurfaceSize);

                TargetingWindow targetingWindow = new TargetingWindow(this, 5f);
                OpenWindow(targetingWindow);
            }

            public void Run(double time)
            {
                if (_runCounter >= int.MaxValue) _runCounter = 0;
                _runCounter++;

                if (Time == 0)
                {
                    Time = time;
                    return;
                }

                Update(time);

                if (_runCounter % 5 == 0)
                {
                    Draw();
                }
                
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
                if (_isPaused && _activePopUp == null)
                {
                    PopUp pausePopUp = new PopUp(Bounds.Center - Bounds.Size * 0.75f * 0.5f, Bounds.Size * 0.75f, 10f, 10f, () => !_isPaused, "UI NAV PAUSED", Bounds);
                    OpenPopUp(pausePopUp);
                }
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
                if (_activePopUp != null || _isPaused || !ReferenceEquals(Station, caller))
                {
                    return;
                }
                _activeWindow?.Navigate(input, this);
            }

            public void PauseNavigation(object caller)
            {
                if (!ReferenceEquals(Station, caller))
                {
                    return;
                }
                _activeWindow?.PauseNavigation(this);
            }

            public void ResumeNavigation(object caller)
            {
                if (!ReferenceEquals(Station, caller))
                {
                    return;
                }
                _activeWindow?.ResumeNavigation(this);
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
