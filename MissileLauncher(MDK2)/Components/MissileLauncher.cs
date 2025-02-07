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
            #region Properties
            public Program Program { get; private set; }
            public int ID { get; private set; }
            public string Name { get; private set; }
            public string LauncherTag { get; private set; }
            #endregion

            #region Components
            public List<MissileBay> MissileBays { get; private set; }
            #endregion

            public MissileLauncher(Program program, int id, string name, string launcherTag, int numberOfMissileBays)
            {
                Program = program;
                ID = id;
                Name = name;
                LauncherTag = launcherTag;

                MissileBays = new List<MissileBay>();
                for (int i = 0; i < numberOfMissileBays; i++)
                {
                    MissileBays.Add(new MissileBay(Program, i));
                }
            }

            public void InitNextAvailableMissile()
            {
                MissileBay missileBay = MissileBays.Find(x => x.State == MissileBay.Status.Exists);
                missileBay?.InitMissile(LauncherTag);
            }

            public void LaunchNextAvailableMissile(long targetID)
            {
                MissileBay missileBay = MissileBays.Find(x => x.State == MissileBay.Status.Ready);
                missileBay?.Launch(targetID);
            }
        }
    }
}
