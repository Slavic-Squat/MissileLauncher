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
            public string ID { get; private set; }
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
            private double _lastRunTime;
            public ControlStation(string id, UICoordinator uiCoordinator)
            {
                ID = id.ToUpper();
                _uiCoordinator = uiCoordinator;

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                _controllerReference = AllBlocks.Where(b => b is IMyShipController && b.CustomName.ToUpper().Contains($"CONTROL STATION {ID}")).FirstOrDefault() as IMyShipController;
                if (_controllerReference == null)
                {
                    throw new Exception($"No controller found for Control Station {ID}!");
                }
                float internalSurfaceCount = (_controllerReference as IMyTextSurfaceProvider)?.SurfaceCount ?? 0;
                for (int i = 0; i < internalSurfaceCount; i++)
                {
                    _displays.Add((_controllerReference as IMyTextSurfaceProvider).GetSurface(i));
                }
                IEnumerable<IMyTextPanel> additionalDisplays = AllBlocks.Where(b => b is IMyTextPanel && b.CustomName.ToUpper().Contains($"CONTROL STATION {ID} DISPLAY")).Cast<IMyTextPanel>();
                _displays.AddRange(additionalDisplays);
                if (_displays.Count == 0)
                {
                    throw new Exception($"No displays found for Control Station {ID}!");
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
                if (_lastRunTime == 0)
                {
                    _lastRunTime = time;
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
                _lastRunTime = time;
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

                PopUp popUp = new PopUp(PrimaryDisplay, _ui.Bounds.Center - _ui.Bounds.Size * 0.75f * 0.5f, _ui.Bounds.Size * 0.75f, 10f, 10f, () => !IsControlling, "UI NAV PAUSED\nREASON: CONTROLLING OBJECT", _ui.Bounds);
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
                    PopUp pausePopUp = new PopUp(PrimaryDisplay, _ui.Bounds.Center - _ui.Bounds.Size * 0.75f * 0.5f, _ui.Bounds.Size * 0.75f, 10f, 10f, () => !_isPaused, "UI NAV PAUSED\nREASON: USER PAUSED", _ui.Bounds);
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
