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
            public static ControlPanel CreateTargetingOptionsPanel(Vector2 pos, RadarWindow window, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;

                int numButtons = 4;
                float padding = 15f;
                float spacing = 10f;

                Vector2 labelSize = new Vector2(125f, 40f);
                Vector2 buttonSize = new Vector2(125f, 35f);

                float totalWidth = labelSize.X + 2 * padding;
                float totalHeight = labelSize.Y + numButtons * buttonSize.Y + (numButtons - 1) * spacing + 2 * padding;
                Vector2 panelSize = new Vector2(totalWidth, totalHeight);
                Vector2 panelPos = pos;
                if (horCent) panelPos.X -= panelSize.X / 2f;
                if (vertCent) panelPos.Y -= panelSize.Y / 2f;

                ControlPanel panel = new ControlPanel(panelPos, panelSize, 5f, 2.5f);

                Vector2 labelPos = panel.Pos + new Vector2(panel.Size.X / 2f - labelSize.X / 2f, padding);
                RectangleF labelBounds = new RectangleF(labelPos, labelSize);
                MySprite labelSprite = SpriteHelper.CreateText(labelBounds, "OPTIONS\n------------------", Color.White, surface, TextAlignment.CENTER, true, 0);
                panel.AddSprite(labelSprite);

                Vector2 buttonPos = panel.Pos + new Vector2(panel.Size.X / 2f - buttonSize.X / 2f, padding + labelSize.Y);

                Func<string> getText = () => "SCALE: " + GetName(window.ScopeScale);
                Func<bool> action = () => { window.ScopeScale = NextScopeScale(window.ScopeScale); return true; };

                Button button = new Button("ScopeScale", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, window.Display);
                panel.AddButton(button);

                buttonPos.Y += buttonSize.Y + spacing;

                getText = () => "TYPE: " + GetName(window.NavTypeFilter);
                action = () => { window.NavTypeFilter = NextEntityTypeFilter(window.NavTypeFilter); return true; };

                button = new Button("TypeFilter", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, window.Display);
                panel.AddButton(button);

                buttonPos.Y += buttonSize.Y + spacing;

                getText = () => "REL: " + GetName(window.NavRelationFilter);
                action = () => { window.NavRelationFilter = NextEntityRelationFilter(window.NavRelationFilter); return true; };

                button = new Button("RelationFilter", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, window.Display);
                panel.AddButton(button);

                buttonPos.Y += buttonSize.Y + spacing;

                getText = () => "SRC: " + GetName(window.NavSourceFilter);
                action = () => { window.NavSourceFilter = NextEntitySourceFilter(window.NavSourceFilter); return true; };

                button = new Button("SourceFilter", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, window.Display);
                panel.AddButton(button);

                return panel;
            }

            public static Menu CreateEntityMenu(Vector2 pos, EntityInfoExt entity, RadarWindow window, UIWireManager wireManager, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;

                Vector2 buttonSize = new Vector2(125f, 35f);
                float padding = 15f;
                float spacing = 10f;

                if (entity.Type == EntityType.Missile && entity.Relation == EntityRelation.Me)
                {
                    int numButtons = 1;
                    float totalWidth = numButtons * buttonSize.X + (numButtons - 1) * spacing + 2 * padding;
                    float totalHeight = buttonSize.Y + 2 * padding;

                    Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                    Vector2 menuPos = pos;
                    if (horCent) menuPos.X -= menuSize.X / 2f;
                    if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                    Menu menu = new Menu(menuPos, menuSize, 5f);

                    Vector2 buttonPos = menu.Pos + padding;
                    Func<string> getText = () => "ABORT";
                    Func<bool> action = () =>
                    {
                        return true;
                    };
                    Button button = new Button("Abort", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    return menu;
                }
                else if (entity.Source == EntitySource.Remote)
                {
                    int numButtons = 3;
                    float totalWidth = numButtons * buttonSize.X + (numButtons - 1) * spacing + 2 * padding;
                    float totalHeight = buttonSize.Y + 2 * padding;
                    Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                    Vector2 menuPos = pos;
                    if (horCent) menuPos.X -= menuSize.X / 2f;
                    if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                    Menu menu = new Menu(menuPos, menuSize, 5f);
                    Vector2 buttonPos = menu.Pos + padding;
                    Func<string> getText = () => "VIEW";
                    Func<bool> action = () =>
                    {
                        return true;
                    };
                    Button button = new Button("View", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    buttonPos.X += buttonSize.X + spacing;
                    getText = () => "FIRE MISL";
                    action = () =>
                    {
                        Vector2 missileMenuPos = window.Center;
                        ModalMenu missileMenu = CreateMissileArmMenu(missileMenuPos, window, wireManager, true, true);
                        window.CloseMenu(menu);
                        window.OpenMenu(missileMenu);
                        return true;
                    };
                    button = new Button("FireMissile", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    buttonPos.X += buttonSize.X + spacing;

                    getText = () => "SET REL";
                    action = () =>
                    {
                        return true;
                    };

                    button = new Button("SetRelation", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    return menu;

                }
                else
                {
                    int numButtons = 4;
                    float totalWidth = numButtons * buttonSize.X + (numButtons - 1) * spacing + 2 * padding;
                    float totalHeight = buttonSize.Y + 2 * padding;
                    Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                    Vector2 menuPos = pos;
                    if (horCent) menuPos.X -= menuSize.X / 2f;
                    if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                    Menu menu = new Menu(menuPos, menuSize, 5f);
                    Vector2 buttonPos = menu.Pos + padding;
                    Func<string> getText = () => "VIEW";
                    Func<bool> action = () =>
                    {
                        return true;
                    };
                    Button button = new Button("View", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    buttonPos.X += buttonSize.X + spacing;
                    getText = () => "FIRE MISL";
                    action = () =>
                    {
                        Vector2 missileMenuPos = window.Center;
                        ModalMenu missileMenu = CreateMissileArmMenu(missileMenuPos, window, wireManager, true, true);
                        window.CloseMenu(menu);
                        window.OpenMenu(missileMenu);
                        return true;
                    };
                    button = new Button("FireMissile", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    buttonPos.X += buttonSize.X + spacing;

                    getText = () => "SET REL";
                    action = () =>
                    {
                        return true;
                    };

                    button = new Button("SetRelation", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    buttonPos.X += buttonSize.X + spacing;

                    getText = () => "FORGET";
                    action = () =>
                    {
                        return true;
                    };

                    button = new Button("Forget", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    return menu;
                }
            }

            public static ModalMenu CreateMissileArmMenu(Vector2 pos, RadarWindow window, UIWireManager wireManger, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;

                int numBays = 25;
                int numColumns = 5;
                int numRows = 2;

                float padding = 20f;
                float spacing = 10f;

                Vector2 labelSize = new Vector2(150f, 30f);
                Vector2 panelSize = new Vector2(175f, 200f);
                Vector2 buttonSize = new Vector2(125f, 35f);

                float totalWidth = panelSize.X * numColumns + spacing * (numColumns - 1) + 2 * padding;
                float totalHeight = labelSize.Y + panelSize.Y * numRows + spacing * (numRows - 1) + 2 * padding;

                Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                Vector2 menuPos = pos;
                if (horCent) menuPos.X -= menuSize.X / 2f;
                if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                ModalMenu menu = new ModalMenu(menuPos, menuSize, 5f, () => false, surface);

                Vector2 labelPos = menu.Pos + new Vector2(menu.Size.X / 2f - labelSize.X / 2f, padding);
                RectangleF labelBounds = new RectangleF(labelPos, labelSize);
                MySprite labelSprite = SpriteHelper.CreateText(labelBounds, "MISSILE BAYS\n------------------", Color.White, window.Display, TextAlignment.CENTER, true, 0);
                menu.AddSprite(labelSprite, -1);
                
                int bayIndex = 0;

                int numPages = (int)Math.Ceiling((float)numBays / (numRows * numColumns));

                for (int i = 0; i < numPages; i++)
                {
                    Vector2 pageLabelSize = new Vector2(50f, 30f);
                    Vector2 pageLabelPos = menu.Pos + new Vector2(menu.Size.X - padding - pageLabelSize.X, padding);
                    RectangleF pageLabelBounds = new RectangleF(pageLabelPos, pageLabelSize);
                    MySprite pageLabelSprite = SpriteHelper.CreateText(pageLabelBounds, $"PAGE: {i + 1} / {numPages}", Color.White, surface, TextAlignment.LEFT, true, 0);
                    menu.AddSprite(pageLabelSprite, i);

                    for (int j = 0; j < numRows; j++)
                    {
                        Vector2 panelPos = Vector2.Zero;
                        panelPos.Y = menu.Pos.Y + padding + labelSize.Y + (panelSize.Y + spacing) * j;

                        for (int k = 0; k < numColumns; k++)
                        {
                            if (bayIndex >= numBays) break;
                            panelPos.X = menu.Pos.X + padding + (panelSize.X + spacing) * k;
                            Func<string> getText = () => $"Bay [{bayIndex}]\n-------------------";
                            InfoPanel panel = new InfoPanel(panelPos, panelSize, 5f, getText, surface);
                            menu.AddInfoPanel(panel, i);

                            Vector2 buttonPos = panel.Pos + new Vector2(panel.Size.X * 0.5f - buttonSize.X * 0.5f, panel.Size.Y - buttonSize.Y - 10f);
                            getText = () => "ARM";
                            Func<bool> action = () =>
                            {
                                return true;
                            };
                            Button button = new Button("Arm", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                            menu.AddButton(button, i);
                            bayIndex++;
                        }
                    }
                }
                return menu;
            }
        }
    }
}
