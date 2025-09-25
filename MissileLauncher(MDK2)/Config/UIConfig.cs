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
        public static class UIConfig
        {
            public static Color WindowBackgroundColor = new Color(0, 0, 0, 255);
            public static Color WindowBorderColor = new Color(20, 20, 20, 255);
            public static Color WindowBorderColorActive = new Color(0, 76, 255, 255);
            public static Color WindowHighlightColor = new Color(255, 200, 0, 255);

            public static Color MenuBackgroundColor = new Color(10, 10, 10, 255);
            public static Color MenuBorderColor = new Color(20, 20, 20, 255);
            public static Color MenuBorderColorFocused = new Color(0, 76, 255, 255);
            public static Color MenuHighlightColor = new Color(255, 200, 0, 255);

            public static Color ButtonBackgroundColor = new Color(32, 32, 32, 255);
            public static Color ButtonBackgroundColorPressed = new Color(12, 20, 38, 255);
            public static Color ButtonBackgroundColorOff = new Color(51, 15, 26, 255);
            public static Color ButtonBackgroundColorOn = new Color(0, 0, 0, 255);
            public static Color ButtonBorderColor = new Color(128, 128, 128, 255);
            public static Color ButtonBorderColorPressed = new Color(0, 76, 255, 255);
            public static Color ButtonBorderColorOff = new Color(255, 0, 76, 255);
            public static Color ButtonBorderColorOn = new Color(0, 0, 0, 255);
            public static Color ButtonTextColor = new Color(255, 255, 255, 255);
            public static Color ButtonTextColorPressed = new Color(0, 76, 255, 255);
            public static Color ButtonTextColorOff = new Color(255, 0, 76, 255);
            public static Color ButtonTextColorOn = new Color(0, 0, 0, 255);
            public static Color ButtonHighlightColor = new Color(255, 200, 0, 255);
        }
    }
}
