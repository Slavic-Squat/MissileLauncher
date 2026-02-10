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
        public class PanelPage
        {
            public int PageIndex { get; private set; }
            public List<IButton> Buttons { get; private set; } = new List<IButton>();
            public List<IUpdatable> Updateables { get; private set; } = new List<IUpdatable>();
            public List<IUIElement> UIElements { get; private set; } = new List<IUIElement>();
            public List<MySprite> Sprites { get; private set; } = new List<MySprite>();


            public PanelPage(int pageIndex)
            {
                PageIndex = pageIndex;
            }

            public void AddButton(IButton button)
            {
                Buttons.Add(button);
                Updateables.Add(button);
                UIElements.Add(button);
            }

            public void AddInfoPanel(InfoPanel panel)
            {
                UIElements.Add(panel);
            }

            public void AddSprite(MySprite sprite)
            {
                Sprites.Add(sprite);
            }
        }
    }
}
