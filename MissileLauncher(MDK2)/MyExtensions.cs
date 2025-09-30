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
    public static class MyExtensions
    {
        public static Enum Next(this Enum enumValue)
        {
            Type enumType = enumValue.GetType();
            Array values = Enum.GetValues(enumType);
            int currentIndex = Array.IndexOf(values, enumValue);
            int nextIndex = (currentIndex + 1) % values.Length;
            return (Enum)values.GetValue(nextIndex);
        }

        public static Enum Previous(this Enum enumValue)
        {
            Type enumType = enumValue.GetType();
            Array values = Enum.GetValues(enumType);
            int currentIndex = Array.IndexOf(values, enumValue);
            int prevIndex = (currentIndex - 1 + values.Length) % values.Length;
            return (Enum)values.GetValue(prevIndex);
        }
    }
}
