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
        public class ControlStation : IController
        {
            public int ID { get; private set; }
            public double Time { get; private set; }
            public bool HasFireControl { get; private set; }
            public IControllable Controllable { get; private set; }
            public UserInput UserInput { get; private set; }
            public bool IsControlling => Controllable != null;
            public IMyTextSurface PrimaryDisplay { get; private set; }

            private List<IMyTextSurface> _displays = new List<IMyTextSurface>();
            private IMyShipController _controllerReference;
            private UI _ui;
            private UICoordinator _uiCoordinator;
            private bool _isPaused = false;
            public ControlStation(int iD, UICoordinator uiCoordinator)
            {
                ID = iD;
                _uiCoordinator = uiCoordinator;

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                _controllerReference = AllGridBlocks.Find(b => b is IMyShipController && b.CustomName.Contains($"Control Station {ID}")) as IMyShipController;
                if (_controllerReference == null)
                {
                    DebugWrite($"Error: No controller found for Control Station {ID}!\n", true);
                    throw new Exception($"No controller found for Control Station {ID}!\n");
                }
                float internalSurfaceCount = (_controllerReference as IMyTextSurfaceProvider)?.SurfaceCount ?? 0;
                for (int i = 0; i < internalSurfaceCount; i++)
                {
                    _displays.Add((_controllerReference as IMyTextSurfaceProvider).GetSurface(i));
                }
                IEnumerable<IMyTextPanel> additionalDisplays = AllGridBlocks.Where(b => b is IMyTextPanel && b.CustomName.Contains($"Control Station {ID} Display")).Cast<IMyTextPanel>();
                _displays.AddRange(additionalDisplays);
                if (_displays.Count == 0)
                {
                    DebugWrite($"Error: No displays found for Control Station {ID}!\n", true);
                    throw new Exception($"No displays found for Control Station {ID}!\n");
                }
            }

            private void Init()
            {
                PrimaryDisplay = _displays[0];
                UserInput = new UserInput(_controllerReference);
                _ui = new UI(this, PrimaryDisplay, _uiCoordinator);
            }

            public void Run(double time)
            {
                if (Time == 0)
                {
                    Time = time;
                    return;
                }

                UserInput.Run(time);
                _ui.Run(time);

                if (!_isPaused && !IsControlling)
                {
                    _ui.Navigate(UserInput, this);
                }
                else if (!_isPaused && IsControlling)
                {
                    Controllable.Control(UserInput, this);
                }
                Time = time;
            }

            public void TakeFireControl(MissileCoordinator coordinator)
            {
                if (!coordinator.FireControlAvail)
                {
                    return;
                }
                coordinator.GiveFireControl(this);
                HasFireControl = true;
            }

            public void ReleaseFireControl(MissileCoordinator coordinator)
            {
                coordinator.RevokeFireControl(this);
                HasFireControl = false;
            }

            public void TakeControl(IControllable controllable)
            {
                if (IsControlling)
                {
                    ReleaseControl(Controllable);
                }
                if (controllable == null || controllable.IsUnderControl)
                {
                    return;
                }
                controllable.GiveControl(this);
                controllable.RequestRelease += ReleaseControl;
                Controllable = controllable;

                PopUp popUp = new PopUp(_ui.Bounds.Center - _ui.Bounds.Size * 0.75f * 0.5f, _ui.Bounds.Size * 0.75f, 10f, 10f, () => !IsControlling, $"UI NAV PAUSED\nREASON: CONTROLLING OBJECT", _ui.Display);
                _ui.OpenPopUp(popUp);
                _ui.PauseNavigation(this);
            }

            public void ReleaseControl(IControllable controllable)
            {
                if (controllable == null || !ReferenceEquals(Controllable, controllable))
                {
                    return;
                }
                controllable.RevokeControl(this);
                controllable.RequestRelease -= ReleaseControl;
                Controllable = null;
                _ui.ResumeNavigation(this);
            }

            public void PauseControl()
            {
                _isPaused = true;
                if (IsControlling)
                {
                    Controllable.PauseControl(this);
                }
                else
                {
                    PopUp pausePopUp = new PopUp(_ui.Bounds.Center - _ui.Bounds.Size * 0.75f * 0.5f, _ui.Bounds.Size * 0.75f, 10f, 10f, () => !_isPaused, "UI NAV PAUSED\nREASON: USER PAUSED", _ui.Display);
                    _ui.OpenPopUp(pausePopUp);
                    _ui.PauseNavigation(this);
                }
            }

            public void ResumeControl()
            {
                _isPaused = false;
                if (IsControlling)
                {
                    Controllable.ResumeControl(this);
                }
                else
                {
                    _ui.ResumeNavigation(this);
                }
            }
        }
    }
}
