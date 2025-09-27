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

            private List<IMyTextSurface> _displays = new List<IMyTextSurface>();
            private IMyShipController _controllerReference;
            private UI _ui;
            private UIWireManager _uiWireManager;
            private Program _program;
            public ControlStation(Program program, int iD, UIWireManager uiWireManager)
            {
                _program = program;
                ID = iD;
                _uiWireManager = uiWireManager;

                TryGetBlocks();

                Input = new UserInput(_controllerReference);
                _ui = new UI(this, _displays[0], _uiWireManager);
            }

            public bool TryGetBlocks()
            {
                try
                {
                    _controllerReference = _program.GridTerminalSystem.GetBlockWithName($"Control Station [{ID}]") as IMyShipController;
                    if (_controllerReference == null)
                    {
                        throw new Exception();
                    }
                    float internalSurfaceCount = (_controllerReference as IMyTextSurfaceProvider)?.SurfaceCount ?? 0;
                    for (int i = 0; i < internalSurfaceCount; i++)
                    {
                        _displays.Add((_controllerReference as IMyTextSurfaceProvider).GetSurface(i));
                    }
                    _program.GridTerminalSystem.GetBlockGroupWithName($"Control Station [{ID}] Displays")?.GetBlocksOfType(_displays);
                    return true;
                }
                catch (Exception ex)
                {
                    _program.Echo("Error in Control Station Construction");
                    return false;
                    throw;
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
            }

            public void TakeControl(IControllable controllable)
            {
                if (IsControlling)
                {
                    ReleaseControl();
                }
                Controllable = controllable;
                controllable.AssignControl(this);
            }

            public void ReleaseControl()
            {
                Controllable?.UnAssignControl();
                Controllable = null;
            }
        }
    }
}
