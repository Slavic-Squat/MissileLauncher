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
        public interface IControllable
        {
            IController Controller { get; }
            bool IsControlPaused { get; }
            bool IsUnderControl { get; }
            event Action<IControllable> RequestRelease;

            void Control(UserInput input, object caller);
            void GiveControl(IController controller);
            void RevokeControl(IController controller);
            void PauseControl();
            void ResumeControl();
        }
    }
}
