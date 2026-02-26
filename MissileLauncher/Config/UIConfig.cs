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
            public static Color DarkGray0 = new Color(20, 20, 20, 255);
            public static Color DarkGray1 = new Color(32, 32, 32, 255);
            public static Color DarkGray2 = new Color(48, 48, 48, 255);
            public static Color DarkGray3 = new Color(64, 64, 64, 255);
            public static Color LightGray0 = new Color(192, 192, 192, 255);
            public static Color Blue0 = new Color(0, 153, 255, 255);
            public static Color Yellow0 = new Color(255, 208, 0, 255);
            public static Color Red0 = new Color(255, 64, 64, 255);

            public static Color WindowFillColor = DarkGray0;
            public static Color WindowBorderColor = LightGray0;
            public static Color WindowBorderColorActive = Blue0;

            public static Color MenuFillColor = DarkGray0;
            public static Color MenuFillColorActive = DarkGray0;
            public static Color MenuBorderColor = LightGray0;
            public static Color MenuBorderColorActive = Blue0;

            public static Color PanelFillColor = DarkGray0;
            public static Color PanelFillColorHighlighted = DarkGray0;
            public static Color PanelFillColorActive = DarkGray0;
            public static Color PanelBorderColor = LightGray0;
            public static Color PanelBorderColorActive = Blue0;
            public static Color PanelHighlightColor = Yellow0;

            public static Color ButtonFillColor = DarkGray0;
            public static Color ButtonFillColorHighlighted = DarkGray0;
            public static Color ButtonFillColorPressed = DarkGray0;
            public static Color ButtonFillColorDisabled = DarkGray0;
            public static Color ButtonBorderColor = LightGray0;
            public static Color ButtonBorderColorPressed = Color.GreenYellow;
            public static Color ButtonBorderColorDisabled = DarkGray3;
            public static Color ButtonTextColor = LightGray0;
            public static Color ButtonTextColorPressed = Color.GreenYellow;
            public static Color ButtonTextColorDisabled = DarkGray3;
            public static Color ButtonHighlightColor = Yellow0;

            public static Color ToggleButtonFillColorReleased = DarkGray0;
            public static Color ToggleButtonFillColorRH = DarkGray0;
            public static Color ToggleButtonFillColorPressed = DarkGray0;
            public static Color ToggleButtonFillColorPH = DarkGray0;
            public static Color ToggleButtonBorderColorReleased = Red0;
            public static Color ToggleButtonBorderColorPressed = Color.GreenYellow;
            public static Color ToggleButtonTextColorReleased = Red0;
            public static Color ToggleButtonTextColorPressed = Color.GreenYellow;

            public static Color FriendlyColor = Color.GreenYellow;
            public static Color NeutralColor = Color.Yellow;
            public static Color HostileColor = Red0;
            public static Color MeColor = Color.CornflowerBlue;
        }
    }
}
