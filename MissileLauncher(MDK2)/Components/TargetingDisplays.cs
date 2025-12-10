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
            private TargetingSpriteBuilderSimple _spriteBuilder;
            private List<IMyTextSurface> _displays = new List<IMyTextSurface>();

            private Dictionary<long, EntityInfoExt> _entityInfo = new Dictionary<long, EntityInfoExt>();
            private int _runCounter;
            public TargetingDisplays(Dictionary<long, EntityInfoExt> entityInfo)
            {
                _spriteBuilder = new TargetingSpriteBuilderSimple();
                _entityInfo = entityInfo;

                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                IEnumerable<IMyTerminalBlock> temp = AllGridBlocks.Where(b => b is IMyTextSurface && b.CustomName.Contains("Targeting Display"));

                foreach (var displayBlock in temp)
                {
                    AddDisplay(displayBlock as IMyTextSurface);
                }

                IMyTextSurfaceProvider consoleBlock = AllGridBlocks.Find(b => b is IMyTextSurfaceProvider && b.CustomName.Contains("Targeting Console")) as IMyTextSurfaceProvider;
                if (consoleBlock != null)
                {
                    AddDisplay(consoleBlock.GetSurface(0));
                }
            }

            private void Init()
            {
                foreach (var display in _displays)
                {
                    display.ContentType = ContentType.SCRIPT;
                    display.Script = "";
                    display.ScriptBackgroundColor = Color.Black;
                }
            }

            public void Draw()
            {
                if (_runCounter++ % 10 == 0)
                {
                    Dictionary<long, MyEntitySprite> dump = new Dictionary<long, MyEntitySprite>();
                    List<MySpriteExt> sprites = _spriteBuilder.BuildSprites(_entityInfo, out dump);

                    foreach (var display in _displays)
                    {
                        var frame = display.DrawFrame();
                        sprites.ForEach(sprite => sprite.Draw(frame));
                        frame.Dispose();
                    }
                }                
            }

            public void AddDisplay(IMyTextSurface display)
            {
                _displays.Add(display);
            }
        }
    }
}
