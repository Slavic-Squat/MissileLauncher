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
            public static ControlPanel CreateTargetingOptionsPanel(RadarWindow window, Vector2 pos, Vector2 size)
            {
                IMyTextSurface surface = window.Display;

                RectangleF panelBounds = new RectangleF(pos, size);
                ControlPanel panel = new ControlPanel(panelBounds.Position, panelBounds.Size);

                float padding = 0.1f;
                float minDim = Math.Min(panelBounds.Width, panelBounds.Height);

                RectangleF elementSpace = new RectangleF(pos + minDim * padding, size - minDim * padding * 2f);

                Vector2 elementPos = elementSpace.Position;
                Vector2 elementSize = new Vector2(elementSpace.Size.X, elementSpace.Size.Y * 0.2f);

                RectangleF labelBounds = new RectangleF(elementPos, elementSize);
                MySprite labelSprite = SpriteHelper.CreateText(labelBounds, "OPTIONS\n------------------", Color.White, surface, TextAlignment.CENTER, true, 0);
                panel.AddSprite(labelSprite);

                elementPos.Y += elementSize.Y;
                float remainingHeight = elementSpace.Bottom - elementPos.Y;

                int maxButtons = 4;
                float spacingRatio = 0.25f;

                float buttonHeight = UIUtilities.CalculateElementSize(remainingHeight, maxButtons, spacingRatio);
                float spacing = buttonHeight * spacingRatio;

                elementSize.Y = buttonHeight;

                Func<string> getText = () => "SCALE: " + GetName(window.ScopeScale);
                Func<bool> action = () => { window.ScopeScale = NextScopeScale(window.ScopeScale); return true; };

                Button button = new Button("ScopeScale", elementPos, elementSize, getText, action, window.Display);
                panel.AddButton(button);

                elementPos.Y += buttonHeight + spacing;

                getText = () => "TYPE: " + GetName(window.NavTypeFilter);
                action = () => { window.NavTypeFilter = NextEntityTypeFilter(window.NavTypeFilter); return true; };

                button = new Button("TypeFilter", elementPos, elementSize, getText, action, window.Display);
                panel.AddButton(button);

                elementPos.Y += buttonHeight + spacing;

                getText = () => "REL: " + GetName(window.NavRelationFilter);
                action = () => { window.NavRelationFilter = NextEntityRelationFilter(window.NavRelationFilter); return true; };

                button = new Button("RelationFilter", elementPos, elementSize, getText, action, window.Display);
                panel.AddButton(button);

                elementPos.Y += buttonHeight + spacing;

                getText = () => "SRC: " + GetName(window.NavSourceFilter);
                action = () => { window.NavSourceFilter = NextEntitySourceFilter(window.NavSourceFilter); return true; };

                button = new Button("SourceFilter", elementPos, elementSize, getText, action, window.Display);
                panel.AddButton(button);

                return panel;
            }

            public static Menu CreateEntityMenu(RadarWindow window, Vector2 pos, Vector2 size, EntityInfoExt entity, UIWireManager wireManager)
            {
                IMyTextSurface surface = window.Display;

                RectangleF menuBounds = new RectangleF(pos, size);
                float padding = 0.2f;
                float minDim = Math.Min(menuBounds.Width, menuBounds.Height);

                RectangleF elementSpace = new RectangleF(pos + minDim * padding, size - minDim * padding * 2f);

                int maxButtons = 4;
                float spacingRatio = 0.25f;
                float buttonWidth = UIUtilities.CalculateElementSize(elementSpace.Width, maxButtons, spacingRatio);
                float buttonHeight = elementSpace.Height;
                float spacing = buttonWidth * spacingRatio;

                if (true)
                {
                    int numberOfButtons = 1;
                    Vector2 usedSpace = new Vector2(numberOfButtons * buttonWidth + (numberOfButtons - 1) * spacing, buttonHeight);
                    Vector2 freeSpace = elementSpace.Size - usedSpace;
                    elementSpace.Position += 0.5f * freeSpace;
                    elementSpace.Size = usedSpace;
                    menuBounds.Position += 0.5f * freeSpace;
                    menuBounds.Size -= freeSpace;

                    Menu menu = new Menu(menuBounds.Position, menuBounds.Size);

                    Vector2 elementSize = new Vector2(buttonWidth, buttonHeight);
                    Vector2 elementPos = elementSpace.Position;

                    Func<string> getText = () => "ABORT";
                    Func<bool> action = () =>
                    {
                        return true;
                    };
                    Button button = new Button("Abort", elementPos, elementSize, getText, action, window.Display);
                    menu.AddButton(button);

                    return menu;
                }
                else
                {
                    int numberOfButtons = 4;
                    Vector2 usedSpace = new Vector2(numberOfButtons * buttonWidth + (numberOfButtons - 1) * spacing, buttonHeight);
                    Vector2 freeSpace = elementSpace.Size - usedSpace;
                    elementSpace.Position += 0.5f * freeSpace;
                    elementSpace.Size = usedSpace;
                    menuBounds.Position += 0.5f * freeSpace;
                    menuBounds.Size -= freeSpace;

                    Menu menu = new Menu(menuBounds.Position, menuBounds.Size);

                    Vector2 elementSize = new Vector2(buttonWidth, elementSpace.Height);
                    Vector2 elementPos = elementSpace.Position + elementSpace.Size - elementSize;

                    Func<string> getText;
                    Func<bool> action;
                    Button button;

                    if (entity.Source == EntitySource.Remote)
                    {
                        getText = () => "FORGET";
                        action = () =>
                        {
                            return true;
                        };

                        button = new Button("Forget", elementPos, elementSize, getText, action, window.Display);
                        menu.AddButton(button);

                        elementPos.X -= buttonWidth + spacing;
                    }

                    getText = () => "FORGET";
                    action = () =>
                    {
                        return true;
                    };

                    button = new Button("Forget", elementPos, elementSize, getText, action, window.Display);
                    menu.AddButton(button);

                    elementPos.X -= buttonWidth + spacing;

                    getText = () => "SET REL";
                    action = () =>
                    {
                        return true;
                    };

                    button = new Button("SetRelation", elementPos, elementSize, getText, action, window.Display);
                    menu.AddButton(button);

                    elementPos.X -= buttonWidth + spacing;

                    getText = () => "FIRE MISSILE";
                    action = () =>
                    {
                        return true;
                    };
                    button = new Button("FireMissile", elementPos, elementSize, getText, action, window.Display);
                    menu.AddButton(button);

                    elementPos.X -= buttonWidth + spacing;
                    getText = () => "VIEW";
                    action = () =>
                    {
                        return true;
                    };

                    button = new Button("View", elementPos, elementSize, getText, action, window.Display);
                    menu.AddButton(button);

                    return menu;
                }
            }
        }
    }
}
