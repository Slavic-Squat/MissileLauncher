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
        public class TargetingUI
        {
            private TargetingSpriteBuilder _targetingSpriteBuilder;
            private IMyTextSurface _display;

            private int _runCounter = 0;

            public TargetingUI(IMyTextSurface display, TargetingSpriteBuilder targetingSpriteBuilder)
            {
                _display = display;
                _targetingSpriteBuilder = targetingSpriteBuilder;

                SetupDrawSurface(_display);
            }

            public void Run(DateTime time)
            {
                _runCounter++;
                _runCounter %= 10;

                if (_runCounter == 9)
                {
                    var frame = _display.DrawFrame();
                    DrawSprites(_display.TextureSize, frame);
                    frame.Dispose();
                }
            }

            public void SetupDrawSurface(IMyTextSurface surface)
            {
                // Draw background color
                surface.ScriptBackgroundColor = new Color(0, 0, 0, 255);

                // Set content type
                surface.ContentType = ContentType.SCRIPT;

                // Set script to none
                surface.Script = "";
            }

            public void DrawSprites(Vector2 screenSize, MySpriteDrawFrame frame)
            {
                foreach (var sprite in _targetingSpriteBuilder.FinalSprites)
                {
                    frame.Add(sprite.ToMySprite(screenSize));
                }
            }
        }
    }
}
