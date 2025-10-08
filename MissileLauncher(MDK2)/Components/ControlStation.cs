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

                TryGetBlocks();

                PrimaryDisplay = _displays[0];
                Input = new UserInput(_controllerReference);
                _ui = new UI(this, PrimaryDisplay, _uiWireManager);
            }

            public bool TryGetBlocks()
            {
                try
                {
                    _controllerReference = GTS.GetBlockWithName($"Control Station [{ID}]") as IMyShipController;
                    if (_controllerReference == null)
                    {
                        throw new Exception();
                    }
                    float internalSurfaceCount = (_controllerReference as IMyTextSurfaceProvider)?.SurfaceCount ?? 0;
                    for (int i = 0; i < internalSurfaceCount; i++)
                    {
                        _displays.Add((_controllerReference as IMyTextSurfaceProvider).GetSurface(i));
                    }
                    GTS.GetBlockGroupWithName($"Control Station [{ID}] Displays")?.GetBlocksOfType(_displays);
                    return true;
                }
                catch (Exception ex)
                {
                    DebugEcho("Error in Control Station Construction");
                    return false;
                    throw;
                }
            }

            public void Run(DateTime time)
            {
                Input.Run(time);
                _ui.Run(time);

                CleanUp();
                if (!IsControlling)
                {
                    _ui.Navigate(Input, time);
                }
            }

            private void CleanUp()
            {
                if (!Controllable?.HasController ?? false)
                {
                    Controllable = null;
                }
            }

            public void TakeControl(IControllable controllable)
            {
                if (IsControlling)
                {
                    ReleaseControl();
                }
                Controllable = controllable;
                controllable.AssignControl(this);

                _ui.OpenModal(new InfoModal((_ui.TextureSize - _ui.SurfaceSize) * 0.5f, _ui.SurfaceSize * 0.75f, 10f, () => !IsControlling, $"UI Navigation Disabled\nReason: Controlling Object", PrimaryDisplay));
            }

            public void ReleaseControl()
            {
                Controllable?.UnAssignControl();
                Controllable = null;
            }
        }
    }
}
