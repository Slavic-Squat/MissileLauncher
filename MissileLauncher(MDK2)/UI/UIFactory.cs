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
        public static class UIFactory
        {
            public static ControlPanel CreateTargetingOptionsPanel(RadarWindow radarWindow, Vector2 pos, Vector2 size, IMyTextSurface surface)
            {
                RectangleF panelBounds = new RectangleF(pos, size);
                ControlPanel panel = new ControlPanel(panelBounds.Position, panelBounds.Size, surface);

                float padding = 0.1f;
                float minDim = Math.Min(panelBounds.Width, panelBounds.Height);

                RectangleF elementSpace = new RectangleF(pos + minDim * padding, size - minDim * padding * 2f);

                Vector2 elementPos = elementSpace.Position;
                Vector2 elementSize = new Vector2(elementSpace.Size.X, elementSpace.Size.Y * 0.1f);

                RectangleF labelBounds = new RectangleF(elementPos, elementSize);
                MySprite labelSprite = SpriteHelper.CreateText(labelBounds, "TARGETING OPTIONS\n------------------", Color.White, surface, TextAlignment.CENTER, true, 0);
                panel.AddSprite(labelSprite);

                elementPos.Y += elementSize.Y + elementSpace.Size.Y * 0.05f;
                elementSize.Y = elementSpace.Size.Y * 0.1f;


                Func<string> getText = () => "SCALE: " + GetName(radarWindow.ScopeScale) + " \u21BB";
                Func<bool> action = () => { radarWindow.ScopeScale = NextScopeScale(radarWindow.ScopeScale); return true; };

                Button button = new Button("ScopeScale", elementPos, elementSize, getText, action, radarWindow.Display);
                panel.AddButton(button);

                elementPos.Y += elementSize.Y + elementSpace.Size.Y * 0.05f;
                elementSize.Y = elementSpace.Size.Y * 0.1f;

                labelBounds = new RectangleF(elementPos, elementSize);
                labelSprite = SpriteHelper.CreateText(labelBounds, "NAVIGATION FILTERS\n-------------------", Color.White, surface, TextAlignment.CENTER, true, 0);
                panel.AddSprite(labelSprite);

                elementPos.Y += elementSize.Y + elementSpace.Size.Y * 0.05f;
                elementSize.Y = elementSpace.Size.Y * 0.1f;

                getText = () => "TYPE: " + GetName(radarWindow.NavTypeFilter) + " \u21BB";
                action = () => { radarWindow.NavTypeFilter = NextEntityTypeFilter(radarWindow.NavTypeFilter); return true; };

                button = new Button("TypeFilter", elementPos, elementSize, getText, action, radarWindow.Display);
                panel.AddButton(button);

                elementPos.Y += elementSize.Y + elementSpace.Size.Y * 0.05f;
                elementSize.Y = elementSpace.Size.Y * 0.1f;

                getText = () => "RELATION: " + GetName(radarWindow.NavRelationFilter) + " \u21BB";
                action = () => { radarWindow.NavRelationFilter = NextEntityRelationFilter(radarWindow.NavRelationFilter); return true; };

                button = new Button("RelationFilter", elementPos, elementSize, getText, action, radarWindow.Display);
                panel.AddButton(button);

                elementPos.Y += elementSize.Y + elementSpace.Size.Y * 0.05f;
                elementSize.Y = elementSpace.Size.Y * 0.1f;

                getText = () => "SOURCE: " + GetName(radarWindow.NavSourceFilter) + " \u21BB";
                action = () => { radarWindow.NavSourceFilter = NextEntitySourceFilter(radarWindow.NavSourceFilter); return true; };

                button = new Button("SourceFilter", elementPos, elementSize, getText, action, radarWindow.Display);
                panel.AddButton(button);

                return panel;
            }
        }
    }
}
