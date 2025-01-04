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
        public class MissileLauncher
        {
            Program program;
            int ID;
            private List<MissileBay> missileBays = new List<MissileBay>();

            public MissileLauncher(Program program, int ID, int numberOfMissileBays)
            {
                this.program = program;
                this.ID = ID;
                for (int i = 0; i < numberOfMissileBays; i++)
                {
                    missileBays.Add(new MissileBay(program, i));
                }
            }

            public void LaunchNextAvailableMissile(int targetID)
            {
                int missileBayIndex = missileBays.FindIndex(x => x.status == MissileBay.Status.Ready);
                if (missileBayIndex != -1)
                {
                    missileBays[missileBayIndex].Launch($"Target {targetID} [{ID}]");
                }
            }


        }
    }
}
