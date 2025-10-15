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
        public interface INavigable : IUIElement
        {
            object Parent { get; }
            bool IsNavigating { get; }
            bool IsPaused { get; }
            event Func<INavigable, bool> RequestStopNavigation;
            bool StartNavigation(object caller);
            bool StopNavigation(object caller);
            bool PauseNavigation();
            bool ResumeNavigation();
            bool Navigate(UserInput input, object caller);
        }
    }
}
