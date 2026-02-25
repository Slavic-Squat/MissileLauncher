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
        public enum BayStatus : byte
        {
            Empty, Building, Fueling, Ready, Launching
        }
        public enum NavMode : byte
        {
            UI, Targeting,
        }
        public enum Direction
        {
            Left, Right, Up, Down, Forward, Backward
        }

        public static class MiscEnumHelper
        {
            public static string GetBayStatusStr(BayStatus status)
            {
                switch (status)
                {
                    case BayStatus.Empty: return "EMPTY";
                    case BayStatus.Building: return "BUILDING";
                    case BayStatus.Fueling: return "FUELING";
                    case BayStatus.Ready: return "READY";
                    case BayStatus.Launching: return "LAUNCHING";
                    default: return "N/A";
                }
            }

            public static string GetNavModeStr(NavMode mode)
            {
                switch (mode)
                {
                    case NavMode.UI: return "UI";
                    case NavMode.Targeting: return "TARGETING";
                    default: return "N/A";
                }
            }

            public static readonly NavMode[] _navModeCycles = new NavMode[] { NavMode.UI, NavMode.Targeting };

            public static NavMode NextNavMode(NavMode mode)
            {
                int index = Array.IndexOf(_navModeCycles, mode);
                if (index < 0) return _navModeCycles[0];
                index = (index + 1) % _navModeCycles.Length;
                return _navModeCycles[index];
            }

            public static NavMode PreviousNavMode(NavMode mode)
            {
                int index = Array.IndexOf(_navModeCycles, mode);
                if (index < 0) return _navModeCycles[0];
                index = (index - 1 + _navModeCycles.Length) % _navModeCycles.Length;
                return _navModeCycles[index];
            }
        }
    }
}
