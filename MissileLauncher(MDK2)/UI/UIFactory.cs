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
                Func<bool> action = () => { window.CycleScopeScale(); return true; };

                Button button = new Button("ScopeScale", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, window.Display);
                panel.AddButton(button);

                buttonPos.Y += buttonSize.Y + spacing;

                getText = () => "TYPE: " + GetName(window.NavTypeFilter);
                action = () => { window.CycleTypeFilter(); return true; };

                button = new Button("TypeFilter", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, window.Display);
                panel.AddButton(button);

                buttonPos.Y += buttonSize.Y + spacing;

                getText = () => "REL: " + GetName(window.NavRelationFilter);
                action = () => { window.CycleRelationFilter(); return true; };

                button = new Button("RelationFilter", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, window.Display);
                panel.AddButton(button);

                buttonPos.Y += buttonSize.Y + spacing;

                getText = () => "SRC: " + GetName(window.NavSourceFilter);
                action = () => { window.CycleSourceFilter(); return true; };

                button = new Button("SourceFilter", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, window.Display);
                panel.AddButton(button);

                return panel;
            }

            public static Menu CreateEntityMenu(Vector2 pos, long targetID, RadarWindow window, UIWireManager wireManager, bool vertCent = false, bool horCent = false)
            {
                var entities = wireManager.GetAllEntities();
                var entity = entities.ContainsKey(targetID) ? entities[targetID] : default(EntityInfoExt);
                if (entity.IsValid) return null;

                IMyTextSurface surface = window.Display;

                Vector2 buttonSize = new Vector2(125f, 35f);
                float padding = 15f;
                float spacing = 15f;

                if (entity.Type == EntityType.Missile && entity.Relation == EntityRelation.Me)
                {
                    int numButtons = 1;
                    float totalWidth = numButtons * buttonSize.X + (numButtons - 1) * spacing + 2 * padding;
                    float totalHeight = buttonSize.Y + 2 * padding;

                    Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                    Vector2 menuPos = pos;
                    if (horCent) menuPos.X -= menuSize.X / 2f;
                    if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                    Func<bool> autoClose = () =>
                    {
                        return window.SelectedEntityID != targetID;
                    };

                    Menu menu = new Menu(menuPos, menuSize, 5f, autoClose);

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

                    Func<bool> autoClose = () =>
                    {
                        return window.SelectedEntityID != targetID;
                    };

                    Menu menu = new Menu(menuPos, menuSize, 5f, autoClose);
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
                        ModalMenu missileMenu = CreateBayMenu(missileMenuPos, targetID, window, wireManager, true, true);
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
                        Vector2 relationMenuPos = window.Pos + new Vector2(window.Size.X * 0.5f, window.Size.Y - 100f);
                        Menu relationMenu = CreateRelationMenu(relationMenuPos, targetID, window, true, true);
                        window.CloseMenu(menu);
                        window.OpenMenu(relationMenu);
                        return true;
                    };

                    button = new Button("SetRelation", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    return menu;

                }
                else
                {
                    int numButtons = 3;
                    float totalWidth = numButtons * buttonSize.X + (numButtons - 1) * spacing + 2 * padding;
                    float totalHeight = buttonSize.Y + 2 * padding;
                    Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                    Vector2 menuPos = pos;
                    if (horCent) menuPos.X -= menuSize.X / 2f;
                    if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                    Func<bool> autoClose = () =>
                    {
                        return window.SelectedEntityID != targetID;
                    };

                    Menu menu = new Menu(menuPos, menuSize, 5f, autoClose);
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
                        ModalMenu missileMenu = CreateBayMenu(missileMenuPos, targetID, window, wireManager, true, true);
                        window.CloseMenu(menu);
                        window.OpenMenu(missileMenu);
                        return true;
                    };
                    button = new Button("FireMissile", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    buttonPos.X += buttonSize.X + spacing;

                    getText = () => "FORGET";
                    action = () =>
                    {
                        wireManager.ForgetTarget(entity.EntityID);
                        return true;
                    };

                    button = new Button("Forget", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button);

                    return menu;
                }
            }

            public static ModalMenu CreateBayMenu(Vector2 pos, long targetID, RadarWindow window, UIWireManager wireManager, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;
                var bays = wireManager.MissileBays;

                int numBays = bays.Count;
                int numColumns = 5;
                int numRows = 2;

                float padding = 20f;
                float spacing = 10f;

                float headerHeight = 100f;
                float footerHeight = 100f;
                Vector2 labelSize = new Vector2(300f, 50f);
                Vector2 panelSize = new Vector2(150f, 175f);
                Vector2 selectButtonSize = new Vector2(100f, 35f);

                float totalWidth = panelSize.X * numColumns + spacing * (numColumns - 1) + 2 * padding;
                float totalHeight = headerHeight + footerHeight + panelSize.Y * numRows + spacing * (numRows - 1);

                Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                Vector2 menuPos = pos;
                if (horCent) menuPos.X -= menuSize.X / 2f;
                if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                Func<bool> canClose = () =>
                {
                    return window.SelectedEntityID != targetID;
                };
                Func<bool> autoClose = () =>
                {
                    return window.SelectedEntityID != targetID;
                };
                ModalMenu menu = new ModalMenu(menuPos, menuSize, 5f, canClose, surface, true, autoClose);

                Vector2 labelPos = menu.Pos + new Vector2(menu.Size.X * 0.5f - labelSize.X * 0.5f, headerHeight * 0.5f - labelSize.Y * 0.5f);
                RectangleF labelBounds = new RectangleF(labelPos, labelSize);
                MySprite labelSprite = SpriteHelper.CreateText(labelBounds, "MISSILE BAYS\n------------------", Color.White, window.Display, TextAlignment.CENTER, true, 0);
                menu.AddSprite(labelSprite, -1);
                
                int bayIndex = 0;

                int numPages = (int)Math.Ceiling((float)numBays / (numRows * numColumns));

                for (int i = 0; i < numPages; i++)
                {
                    Vector2 pageLabelSize = new Vector2(100f, 30f);
                    Vector2 pageLabelPos = menu.Pos + new Vector2(menu.Size.X - padding - pageLabelSize.X, headerHeight * 0.5f - pageLabelSize.Y * 0.5f);
                    RectangleF pageLabelBounds = new RectangleF(pageLabelPos, pageLabelSize);
                    MySprite pageLabelSprite = SpriteHelper.CreateText(pageLabelBounds, $"PAGE: {i + 1} / {numPages}", Color.White, surface, TextAlignment.RIGHT, true, 0);
                    menu.AddSprite(pageLabelSprite, i);

                    for (int j = 0; j < numRows; j++)
                    {
                        Vector2 panelPos = Vector2.Zero;
                        panelPos.Y = menu.Pos.Y + headerHeight + (panelSize.Y + spacing) * j;

                        for (int k = 0; k < numColumns; k++)
                        {
                            if (bayIndex >= numBays) break;
                            var bay = bays[bayIndex];
                            panelPos.X = menu.Pos.X + padding + (panelSize.X + spacing) * k;
                            Func<string> getText = () => bay.ToString();
                            InfoPanel panel = new InfoPanel(panelPos, panelSize, 5f, getText, surface);
                            menu.AddInfoPanel(panel, i);

                            Vector2 buttonPos = panel.Pos + new Vector2(panel.Size.X * 0.5f - selectButtonSize.X * 0.5f, panel.Size.Y - selectButtonSize.Y - 10f);
                            getText = () =>
                            {
                                return bay.IsSelected ? "SELECTED" : "SELECT";
                            };
                            Func<bool> onPress = () =>
                            {
                                wireManager.SelectBay(bay.ID);
                                return true;
                            };
                            Func<bool> onRelease = () =>
                            {
                                wireManager.DeselectBay(bay.ID);
                                return true;
                            };
                            Func<bool> isPressed = () =>
                            {
                                return bay.IsSelected;
                            };
                            Func<bool> canPress = () =>
                            {
                                return bay.IsSelectable;
                            };
                            Func<bool> canRelease = canPress;
                            ToggleButton button = new ToggleButton("SELECT", buttonPos, selectButtonSize, 10f, 3f, 1f, getText, onPress, onRelease, isPressed, surface, canPress, canRelease);
                            menu.AddButton(button, i);
                            bayIndex++;
                        }
                    }
                }

                Vector2 confirmButtonSize = new Vector2(150f, 50f);
                Vector2 confirmButtonPos = menu.Pos + new Vector2(menu.Size.X * 0.5f - confirmButtonSize.X - 20f, menu.Size.Y - footerHeight * 0.5f - confirmButtonSize.Y * 0.5f);
                Func<string> confirmText = () => "CONFIRM";
                Func<bool> action = () =>
                {
                    Vector2 missileFireMenuPos = window.Pos + new Vector2(window.Size.X * 0.5f, window.Size.Y - 100f);
                    ModalMenu missileFireMenu = CreateMissileFireMenu(missileFireMenuPos, targetID, window, wireManager, true, true);
                    window.CloseMenu(menu);
                    window.OpenMenu(missileFireMenu);
                    return true;
                };
                Button confirmButton = new Button("Confirm", confirmButtonPos, confirmButtonSize, 14f, 4f, 1f, confirmText, action, surface);
                menu.AddButton(confirmButton, -1);
                Vector2 cancelButtonSize = new Vector2(150f, 50f);
                Vector2 cancelButtonPos = menu.Pos + new Vector2(menu.Size.X * 0.5f + 20f, menu.Size.Y - footerHeight * 0.5f - confirmButtonSize.Y * 0.5f);
                Func<string> cancelText = () => "CANCEL";
                action = () =>
                {
                    wireManager.ClearSelectedBays();
                    window.CloseMenu(menu);
                    return true;
                };
                Button cancelButton = new Button("Cancel", cancelButtonPos, cancelButtonSize, 14f, 4f, 1f, cancelText, action, surface);
                menu.AddButton(cancelButton, -1);
                return menu;
            }

            public static ModalMenu CreateMissileFireMenu(Vector2 pos, long targetID, RadarWindow window, UIWireManager wireManager, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;

                int numButtons = 2;

                float padding = 20f;
                float spacing = 25f;

                Vector2 buttonSize = new Vector2(150f, 50f);

                float totalWidth = buttonSize.X * numButtons + spacing * (numButtons - 1) + 2 * padding;
                float totalHeight = buttonSize.Y + 2 * padding;

                Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                Vector2 menuPos = pos;
                if (horCent) menuPos.X -= menuSize.X / 2f;
                if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                Func<bool> canClose = () =>
                {
                    return window.SelectedEntityID != targetID;
                };
                Func<bool> autoClose = () =>
                {
                    return window.SelectedEntityID != targetID;
                };

                ModalMenu menu = new ModalMenu(menuPos, menuSize, 5f, canClose, surface, false, autoClose);

                Vector2 buttonPos = menu.Pos + padding;
                Func<string> getText = () => "FIRE";
                Func<bool> action = () =>
                {
                    wireManager.LaunchMissiles(window.SelectedEntityID);
                    window.CloseMenu(menu);
                    return true;
                };
                Button button = new Button("Fire", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                menu.AddButton(button, -1);

                buttonPos.X += buttonSize.X + spacing;
                getText = () => "ABORT";
                action = () =>
                {
                    wireManager.ClearSelectedBays();
                    window.CloseMenu(menu);
                    return true;
                };
                button = new Button("Abort", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                menu.AddButton(button, -1);

                return menu;
            }

            public static Menu CreateRelationMenu(Vector2 pos, long targetID, RadarWindow window, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;

                int numButtons = 3;

                float padding = 15f;
                float spacing = 15f;

                Vector2 buttonSize = new Vector2(125f, 35f);

                float totalWidth = buttonSize.X * numButtons + spacing * (numButtons - 1) + 2 * padding;
                float totalHeight = buttonSize.Y + 2 * padding;

                Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                Vector2 menuPos = pos;
                if (horCent) menuPos.X -= menuSize.X / 2f;
                if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                Func<bool> autoClose = () =>
                {
                    return window.SelectedEntityID != targetID;
                };

                Menu menu = new Menu(menuPos, menuSize, 5f, autoClose);

                Vector2 buttonPos = menu.Pos + padding;
                Func<string> getText = () => "FRNDLY";
                Func<bool> action = () =>
                {
                    return true;
                };
                Button button = new Button("Friendly", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                menu.AddButton(button);

                buttonPos.X += buttonSize.X + spacing;
                getText = () => "NTRL";
                action = () =>
                {
                    return true;
                };
                button = new Button("Neutral", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                menu.AddButton(button);

                buttonPos.X += buttonSize.X + spacing;
                getText = () => "HSTL";
                action = () =>
                {
                    return true;
                };
                button = new Button("Hostile", buttonPos, buttonSize, 8f, 3f, 1f, getText, action, surface);
                menu.AddButton(button);

                return menu;
            }
        }
    }
}
