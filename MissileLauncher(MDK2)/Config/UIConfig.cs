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
            public static Color WindowFillColor = new Color(13, 11, 9, 255);
            public static Color WindowBorderColor = new Color(252, 186, 3, 255);
            public static Color WindowBorderColorActive = new Color(3, 252, 190, 255);

            public static Color MenuFillColor = new Color(26, 23, 17, 255);
            public static Color MenuFillColorActive = new Color(17, 26, 23, 255);
            public static Color MenuBorderColor = new Color(252, 186, 3, 255);
            public static Color MenuBorderColorActive = new Color(3, 252, 190, 255);

            public static Color PanelFillColor = new Color(26, 23, 17, 255);
            public static Color PanelFillColorHighlighted = new Color(38, 35, 25, 255);
            public static Color PanelFillColorActive = new Color(17, 26, 23, 255);
            public static Color PanelBorderColor = new Color(252, 186, 3, 255);
            public static Color PanelBorderColorActive = new Color(3, 252, 190, 255);
            public static Color PanelHighlightColor = new Color(3, 252, 190, 255);

            public static Color ButtonFillColor = new Color(51, 45, 33, 255);
            public static Color ButtonFillColorHighlighted = new Color(77, 64, 38, 255);
            public static Color ButtonFillColorPressed = new Color(19, 38, 32, 255);
            public static Color ButtonFillErrored = new Color(38, 19, 26, 255);
            public static Color ButtonFillColorDisabled = new Color(20, 20, 20, 255);
            public static Color ButtonBorderColor = new Color(252, 186, 3, 255);
            public static Color ButtonBorderColorPressed = new Color(3, 252, 190, 255);
            public static Color ButtonBorderColorErrored = new Color(252, 3, 94, 255);
            public static Color ButtonBorderColorDisabled = new Color(64, 64, 64, 255);
            public static Color ButtonTextColor = new Color(252, 186, 3, 255);
            public static Color ButtonTextColorPressed = new Color(3, 252, 190, 255);
            public static Color ButtonTextColorDisabled = new Color(64, 64, 64, 255);
            public static Color ButtonTextColorErrored = new Color(252, 3, 94, 255);
            public static Color ButtonHighlightColor = new Color(3, 252, 190, 255);

            //public static Color ToggleButtonFillColorReleased = new Color(38, 19, 26, 255);
            //public static Color ToggleButtonFillColorRH = new Color(64, 32, 44, 255);
            //public static Color ToggleButtonBorderColorReleased = new Color(252, 3, 94, 255);
            //public static Color ToggleButtonTextColorReleased = new Color(252, 3, 94, 255);
            public static Color ToggleButtonFillColorReleased = new Color(51, 45, 33, 255);
            public static Color ToggleButtonFillColorRH = new Color(77, 64, 38, 255);
            public static Color ToggleButtonFillColorPressed = new Color(19, 38, 29, 255);
            public static Color ToggleButtonFillColorPH = new Color(32, 64, 49, 255);
            public static Color ToggleButtonBorderColorReleased = new Color(252, 186, 3, 255);
            public static Color ToggleButtonBorderColorPressed = new Color(3, 252, 128, 255);
            public static Color ToggleButtonTextColorReleased = new Color(252, 186, 3, 255);
            public static Color ToggleButtonTextColorPressed = new Color(3, 252, 128, 255);

            public static Color SelectorColor = new Color(252, 186, 3, 255);
            public static Color FriendlyColor = new Color(3, 252, 128, 255);
            public static Color NeutralColor = new Color(252, 186, 3, 255);
            public static Color HostileColor = new Color(252, 3, 94, 255);
            public static Color MeColor = new Color(3, 252, 190, 255);
        }
    }
}
