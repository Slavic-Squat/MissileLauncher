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
            public List<IMyTextSurface> Displays { get; private set; }

            private IMyShipController _controller;
            private Program _program;
            private SystemCoordinator _systemCoordinator;

            Stack<IEnumerator<bool>> _coroutines;
            public ControlStation(Program program, int iD, SystemCoordinator systemCoordinator)
            {
                _program = program;
                ID = iD;

                TryGetBlocks();

                UserInput = new UserInput(_controller);
                _systemCoordinator = systemCoordinator;
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
                    _program.GridTerminalSystem.GetBlockGroupWithName($"Control Station [{ID}] Displays").GetBlocksOfType(Displays);
                    return true;
                }
                catch (Exception ex)
                {
                    _program.Echo("Error in Control Station Construction");
                    return false;
                    throw;
                }
            }
        }
    }
}
