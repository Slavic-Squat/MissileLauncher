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
        public class TargetingDisplays
        {
            private TargetingSpriteBuilderSimple _simpleSpriteBuilder;
            private TargetingSpriteBuilder _advSpriteBuilder;
            private int _numDisplays;

            private List<IMyTextSurface> _advDisplays = new List<IMyTextSurface>();
            private List<IMyTextSurface> _simpleDisplays = new List<IMyTextSurface>();

            private Dictionary<long, EntityInfoExt> _entityInfo = new Dictionary<long, EntityInfoExt>();
            public TargetingDisplays(int numDisplays, Dictionary<long, EntityInfoExt> entityInfo)
            {
                _simpleSpriteBuilder = new TargetingSpriteBuilderSimple();
                _advSpriteBuilder = new TargetingSpriteBuilder();
                _numDisplays = numDisplays;
                _entityInfo = entityInfo;

                GetBlocks();
            }

            private void GetBlocks()
            {
                for (int i = 0; i < _numDisplays; i++)
                {
                    IMyTextPanel displayBlock = AllGridBlocks.Find(b => b.CustomName.Contains($"Targeting Display {i}")) as IMyTextPanel;

                    if (displayBlock == null)
                    {
                        DebugWrite($"Error: Targeting Display {i} not found!\n", true);
                        throw new Exception($"Targeting Display {i} not found!\n");
                    }
                    if (displayBlock.CustomData.Contains("-Advanced"))
                    {
                        AddAdvancedDisplay(displayBlock);
                    }
                    else
                    {
                        AddSimpleDisplay(displayBlock);
                    }
                }
            }

            public void Init()
            {
                foreach (var display in _advDisplays)
                {
                    display.ContentType = ContentType.SCRIPT;
                    display.Script = "";
                    display.ScriptBackgroundColor = Color.Black;
                }
                foreach (var display in _simpleDisplays)
                {
                    display.ContentType = ContentType.SCRIPT;
                    display.Script = "";
                    display.ScriptBackgroundColor = Color.Black;
                }
            }

            public void Run()
            {
                Dictionary<long, MyEntitySprite> dump = new Dictionary<long, MyEntitySprite>();
                List<MySpriteExt> _simpleSprites = _simpleSpriteBuilder.BuildSprites(_entityInfo, out dump);
                List<MySpriteExt> _advSprites = _advSpriteBuilder.BuildSprites(_entityInfo, out dump);

                foreach (var display in _simpleDisplays)
                {
                    var frame = display.DrawFrame();
                    _simpleSprites.ForEach(s => s.Draw(frame));
                    frame.Dispose();
                }
                foreach (var display in _advDisplays)
                {
                    var frame = display.DrawFrame();
                    _advSprites.ForEach(s => s.Draw(frame));
                    frame.Dispose();
                }
            }

            public void AddSimpleDisplay(IMyTextSurface display)
            {
                _simpleDisplays.Add(display);
            }

            public void AddAdvancedDisplay(IMyTextSurface display)
            {
                _advDisplays.Add(display);
            }
        }
    }
}
