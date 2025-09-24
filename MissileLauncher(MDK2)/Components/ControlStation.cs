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
        public class ControlStation
        {
            public UserInput UserInput { get; private set; }
            public int ID { get; private set; }

            private List<IMyTextSurface> _displays = new List<IMyTextSurface>();
            private IMyShipController _controller;
            private TargetingLaser _controlable;
            private Program _program;
            public ControlStation(Program program, int iD)
            {
                _program = program;
                ID = iD;

                TryGetBlocks();

                UserInput = new UserInput(_controller);
            }

            public bool TryGetBlocks()
            {
                try
                {
                    _controller = _program.GridTerminalSystem.GetBlockWithName($"Control Station [{ID}]") as IMyShipController;
                    if (_controller == null)
                    {
                        throw new Exception();
                    }
                    float internalSurfaceCount = (_controller as IMyTextSurfaceProvider)?.SurfaceCount ?? 0;
                    for (int i = 0; i < internalSurfaceCount; i++)
                    {
                        _displays.Add((_controller as IMyTextSurfaceProvider).GetSurface(i));
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
                UserInput.Run(time);
            }

            public void TakeControl(TargetingLaser controlable)
            {
                _controlable = controlable;
                _controlable.TakeControl(this);
            }
        }
    }
}
