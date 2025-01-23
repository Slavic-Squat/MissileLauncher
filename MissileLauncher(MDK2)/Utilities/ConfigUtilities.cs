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
        public static class ConfigUtilties
        {
            public static bool TryWriteToExternalConfig(IMyTerminalBlock storageBlock, string section, string name, string value)
            {
                try
                {
                    MyIni externalConfig = new MyIni();
                    if (!externalConfig.TryParse(storageBlock.CustomData))
                    {
                        throw new Exception();
                    }
                    externalConfig.Set(section, name, value);
                    storageBlock.CustomData = externalConfig.ToString();
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            public static bool TryReadExternalConfig(string externalConfigString, string section, string name, out string value)
            {
                try
                {
                    MyIni externalConfig = new MyIni();
                    if (!externalConfig.TryParse(externalConfigString))
                    {
                        throw new Exception();
                    }
                    value = externalConfig.Get(section, name).ToString();
                    return true;
                }
                catch (Exception ex)
                {
                    value = null;
                    return false;
                }
            }

            public static bool TryQueueExternalCommand(IMyTerminalBlock storageBlock, string value)
            {
                try
                {
                    MyIni externalConfig = new MyIni();
                    if (!externalConfig.TryParse(storageBlock.CustomData))
                    {
                        throw new Exception();
                    }
                    int queuedCommandCounter = externalConfig.Get("Script Info", "Queued Commands Counter").ToInt32();
                    queuedCommandCounter++;
                    externalConfig.Set("Queued Commands", $"{queuedCommandCounter}", value);
                    externalConfig.Set("Script Info", "Queued Commands Counter", $"{queuedCommandCounter}");
                    storageBlock.CustomData = externalConfig.ToString();
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }
    }
}
