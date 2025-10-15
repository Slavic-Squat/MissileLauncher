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
            public DateTime Time { get; private set; }
            public bool HasFireControl { get; private set; }
            public IControllable Controllable { get; private set; }
            public UserInput Input { get; private set; }
            public bool IsControlling => Controllable != null;
            public IMyTextSurface PrimaryDisplay { get; private set; }

            private List<IMyTextSurface> _displays = new List<IMyTextSurface>();
            private IMyShipController _controllerReference;
            private UI _ui;
            private UIWireManager _uiWireManager;
            public ControlStation(int iD, UIWireManager uiWireManager)
            {
                ID = iD;
                _uiWireManager = uiWireManager;

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                _controllerReference = GTS.GetBlockWithName($"Control Station [{ID}]") as IMyShipController;
                if (_controllerReference == null)
                {
                    throw new Exception($"No Controller Found For Control Station [{ID}]");
                }
                float internalSurfaceCount = (_controllerReference as IMyTextSurfaceProvider)?.SurfaceCount ?? 0;
                for (int i = 0; i < internalSurfaceCount; i++)
                {
                    _displays.Add((_controllerReference as IMyTextSurfaceProvider).GetSurface(i));
                }
                List<IMyTextSurface> additionalDisplays = new List<IMyTextSurface>();
                GTS.GetBlockGroupWithName($"Control Station [{ID}] Displays")?.GetBlocksOfType(additionalDisplays);
                _displays.AddRange(additionalDisplays);
                if (_displays.Count == 0)
                {
                    throw new Exception($"No Displays Found For Control Station [{ID}]");
                }
            }

            private void Init()
            {
                PrimaryDisplay = _displays[0];
                Input = new UserInput(_controllerReference);
                _ui = new UI(this, PrimaryDisplay, _uiWireManager);
            }

            public void Run(DateTime time)
            {
                Time = time;
                Input.Run(time);
                _ui.Run(time);

                if (!IsControlling)
                {
                    _ui.Navigate(Input, this);
                }
                else
                {
                    Controllable.Control(Input, this);
                }
            }

            public bool TakeFireControl(MissileCoordinator coordinator)
            {
                if (!coordinator.GiveFireControl(this))
                {
                    return false;
                }
                HasFireControl = true;
                return true;
            }

            public bool ReleaseFireControl(MissileCoordinator coordinator)
            {
                bool success = coordinator.RevokeFireControl(this);
                HasFireControl = false;
                return success;
            }

            public bool TakeControl(IControllable controllable)
            {
                if (IsControlling)
                {
                    ReleaseControl(Controllable);
                }
                if (controllable == null || !controllable.GiveControl(this))
                {
                    return false;
                }
                controllable.RequestRelease += ReleaseControl;
                Controllable = controllable;

                _ui.OpenModal(new InfoModal(_ui.Bounds.Center - _ui.Bounds.Size * 0.75f * 0.5f, _ui.Bounds.Size * 0.75f, 10f, 10f, () => !IsControlling, $"UI Navigation Disabled\nReason: Controlling Object", PrimaryDisplay));
                return true;
            }

            public bool ReleaseControl(IControllable controllable)
            {
                if (controllable == null || !ReferenceEquals(Controllable, controllable))
                {
                    return false;
                }
                controllable.RevokeControl(this);
                controllable.RequestRelease -= ReleaseControl;
                Controllable = null;
                return true;
            }
        }
    }
}
