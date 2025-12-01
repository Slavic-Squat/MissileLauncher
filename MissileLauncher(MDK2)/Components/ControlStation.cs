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
            public ControlStation(int iD, UICoordinator uiCoordinator)
            {
                ID = iD;
                _uiCoordinator = uiCoordinator;

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                string prefix = SystemCoordinator.GridName;
                _controllerReference = GTS.GetBlockWithName($"{prefix} Control Station {ID}") as IMyShipController;
                if (_controllerReference == null)
                {
                    DebugWrite($"Error: No controller found for Control Station {ID} on {prefix}!", true);
                    throw new Exception($"No controller found for Control Station {ID} on {prefix}!");
                }
                float internalSurfaceCount = (_controllerReference as IMyTextSurfaceProvider)?.SurfaceCount ?? 0;
                for (int i = 0; i < internalSurfaceCount; i++)
                {
                    _displays.Add((_controllerReference as IMyTextSurfaceProvider).GetSurface(i));
                }
                List<IMyTextSurface> additionalDisplays = new List<IMyTextSurface>();
                GTS.GetBlockGroupWithName($"{prefix} Control Station {ID} Displays")?.GetBlocksOfType(additionalDisplays);
                _displays.AddRange(additionalDisplays);
                if (_displays.Count == 0)
                {
                    DebugWrite($"Error: No displays found for Control Station {ID} on {prefix}!", true);
                    throw new Exception($"No displays found for Control Station {ID} on {prefix}!");
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
                Time = time;
                UserInput.Run(time);
                _ui.Run(time);

                if (!IsControlling)
                {
                    _ui.Navigate(UserInput, this);
                }
                else
                {
                    Controllable.Control(UserInput, this);
                }
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
                controllable.RequestRelease += ReleaseControl;
                Controllable = controllable;

                _ui.OpenModal(new InfoModal(_ui.Bounds.Center - _ui.Bounds.Size * 0.75f * 0.5f, _ui.Bounds.Size * 0.75f, 10f, 10f, () => !IsControlling, $"UI Navigation Disabled\nReason: Controlling Object", PrimaryDisplay));
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
            }
        }
    }
}
