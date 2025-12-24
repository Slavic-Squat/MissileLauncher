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
            private IReadOnlyDictionary<long, EntityInfoExt> _entityInfo = new Dictionary<long, EntityInfoExt>();
            public TargetingDisplays(IReadOnlyDictionary<long, EntityInfoExt> entityInfo)
            {
                _spriteBuilder = new TargetingSpriteBuilderSimple(new RectangleF(0, 0, 1024f, 1024f));
                _entityInfo = entityInfo;

                GetBlocks();
            }

            private void GetBlocks()
            {
                IEnumerable<IMyTerminalBlock> temp = AllGridBlocks.Where(b => b is IMyTextSurface && b.CustomName.ToUpper().Contains("TARGETING DISPLAY"));

                foreach (var displayBlock in temp)
                {
                    AddDisplay(displayBlock as IMyTextSurface);
                }

                IMyTextSurfaceProvider consoleBlock = AllGridBlocks.Where(b => b is IMyTextSurfaceProvider && b.CustomName.ToUpper().Contains("TARGETING CONSOLE")).FirstOrDefault() as IMyTextSurfaceProvider;
                if (consoleBlock != null)
                {
                    AddDisplay(consoleBlock.GetSurface(0));
                }
            }

            public void Draw()
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

            public void AddDisplay(IMyTextSurface display)
            {
                if (_displays.Contains(display)) return;

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;
                _displays.Add(display);
            }
        }
    }
}
