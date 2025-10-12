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

                PrimaryDisplay = _displays[0];
                Input = new UserInput(_controllerReference);
                _ui = new UI(this, PrimaryDisplay, _uiWireManager);
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

            public void Run(DateTime time)
            {
                Input.Run(time);
                _ui.Run(time);

                if (!IsControlling)
                {
                    _ui.Navigate(Input, time);
                }
                else
                {
                    Controllable.Control(Input, time);
                }
            }

            public void TakeControl(IControllable controllable)
            {
                if (IsControlling)
                {
                    ReleaseControl(Controllable);
                }
                controllable.OnTakeControl();
                controllable.RequestRelease += ReleaseControl;
                Controllable = controllable;

                _ui.OpenModal(new InfoModal(_ui.Bounds.Center - _ui.Bounds.Size * 0.75f * 0.5f, _ui.Bounds.Size * 0.75f, 10f, 10f, () => !IsControlling, $"UI Navigation Disabled\nReason: Controlling Object", PrimaryDisplay));
            }

            public void ReleaseControl(IControllable controllable)
            {
                controllable.OnRelease();
                controllable.RequestRelease -= ReleaseControl;
                if (Controllable == controllable)
                {
                    Controllable = null;
                }
            }
        }
    }
}
