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
            public static ControlPanel CreateTargetingActionsPanel(Vector2 pos, TargetingWindow window, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;
                UIWireManager wireManager = window.UI.UIWireManager;
                ControlStation station = window.UI.Station;
                MissileCoordinator coordinator = wireManager.MissileCoordinator;

                int numButtons = 3;
                float padding = 15f;
                float spacing = 10f;

                Vector2 buttonSize = new Vector2(125f, 35f);

                float totalWidth = numButtons * buttonSize.X + (numButtons - 1) * spacing + 2 * padding;
                float totalHeight = buttonSize.Y + 2 * padding;

                Vector2 panelSize = new Vector2(totalWidth, totalHeight);
                Vector2 panelPos = pos;
                if (horCent) panelPos.X -= panelSize.X / 2f;
                if (vertCent) panelPos.Y -= panelSize.Y / 2f;

                ControlPanel panel = new ControlPanel(window, panelPos, panelSize, 5f, 2.5f);

                Vector2 buttonPos = panel.Pos + new Vector2(padding, padding);

                Func<string> getText = () => "SCALE: " + GetName(window.ScopeScale);
                Func<bool> action = () => window.CycleScopeScale();

                Button button = new Button(buttonPos, buttonSize, 5f, 3f, 2f, getText, action, window.Display);
                panel.AddButton(button, -1);

                buttonPos.X += buttonSize.X + spacing;

                getText = () => "FIRE CTRL";
                Func<bool> onPress = () => station.TakeFireControl(coordinator);
                Func<bool> onRelease = () => station.ReleaseFireControl(coordinator);
                Func<bool> isPressed = () => station.HasFireControl;
                Func<bool> canPress = () => coordinator.FireControlAvail;
                Func<bool> canRelease = () => station.HasFireControl;

                ToggleButton toggleButton = new ToggleButton(buttonPos, buttonSize, 5f, 3f, 2f, getText, onPress, onRelease, isPressed, surface, canPress: canPress, canRelease: canRelease);
                panel.AddButton(toggleButton, -1);

                buttonPos.X += buttonSize.X + spacing;

                getText = () => "SELECT BAYS";
                action = () =>
                {
                    Vector2 bayMenuPos = window.Center;
                    Menu bayMenu = CreateBayMenu(bayMenuPos, window, true, true);
                    return window.OpenMenu(bayMenu);
                };
                canPress = () => station.HasFireControl;
                button = new Button(buttonPos, buttonSize, 5f, 3f, 2f, getText, action, surface, canPress: canPress);
                panel.AddButton(button, -1);

                return panel;
            }
            public static ControlPanel CreateTargetingNavFilterPanel(Vector2 pos, TargetingWindow window, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;

                int numButtons = 3;
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

                ControlPanel panel = new ControlPanel(window, panelPos, panelSize, 5f, 2.5f);

                Vector2 labelPos = panel.Pos + new Vector2(panel.Size.X / 2f - labelSize.X / 2f, padding);
                RectangleF labelBounds = new RectangleF(labelPos, labelSize);
                MySprite labelSprite = SpriteHelper.CreateText(labelBounds, "-NAV FILTER-\n------------------", Color.White, surface, TextAlignment.CENTER, true, 0);
                panel.AddSprite(labelSprite, -1);

                Vector2 buttonPos = panel.Pos + new Vector2(panel.Size.X / 2f - buttonSize.X / 2f, padding + labelSize.Y);

                Func<string> getText = () => "TYPE: " + GetName(window.NavTypeFilter);
                Func<bool> action = () => window.CycleTypeFilter();

                Button button = new Button(buttonPos, buttonSize, 5f, 3f, 2f, getText, action, window.Display);
                panel.AddButton(button, -1);

                buttonPos.Y += buttonSize.Y + spacing;

                getText = () => "REL: " + GetName(window.NavRelationFilter);
                action = () => window.CycleRelationFilter();

                button = new Button(buttonPos, buttonSize, 5f, 3f, 2f, getText, action, window.Display);
                panel.AddButton(button, -1);

                buttonPos.Y += buttonSize.Y + spacing;

                getText = () => "SRC: " + GetName(window.NavSourceFilter);
                action = () => window.CycleSourceFilter();

                button = new Button(buttonPos, buttonSize, 5f, 3f, 2f, getText, action, window.Display);
                panel.AddButton(button, -1);

                return panel;
            }

            public static Menu CreateEntityMenu(Vector2 pos, long targetID, TargetingWindow window, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;
                UIWireManager wireManager = window.UI.UIWireManager;
                ControlStation station = window.UI.Station;
                MissileCoordinator coordinator = wireManager.MissileCoordinator;

                var entities = wireManager.GetAllEntities();
                EntityInfoExt entity;
                if (!entities.TryGetValue(targetID, out entity))
                {
                    return null;
                }             

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
                        return window.SelectedEntityID != targetID || !station.HasFireControl;
                    };

                    Menu menu = new Menu(window, menuPos, menuSize, 5f, autoClose: autoClose);

                    Vector2 buttonPos = menu.Pos + padding;
                    Func<string> getText = () => "ABORT";
                    Func<bool> action = () => wireManager.AbortMissile(targetID, station);
                    Button button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button, -1);

                    return menu;
                }
                else if (entity.Source == EntitySource.Remote)
                {
                    int numButtons = 4;
                    float totalWidth = numButtons * buttonSize.X + (numButtons - 1) * spacing + 2 * padding;
                    float totalHeight = buttonSize.Y + 2 * padding;
                    Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                    Vector2 menuPos = pos;
                    if (horCent) menuPos.X -= menuSize.X / 2f;
                    if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                    Func<bool> autoClose = () => window.SelectedEntityID != targetID;

                    Menu menu = new Menu(window, menuPos, menuSize, 5f, autoClose: autoClose);
                    Vector2 buttonPos = menu.Pos + padding;
                    Func<string> getText = () => "VIEW";
                    Func<bool> action = () => true;
                    Button button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button, -1);

                    buttonPos.X += buttonSize.X + spacing;
                    getText = () => "FIRE MISL";
                    action = () =>
                    {
                        if (coordinator.NumSelectedBays == 0)
                        {
                            Menu missileMenu = CreateBayMenu(window.Center, window, true, true);
                            return window.OpenMenu(missileMenu);
                        }
                        else
                        {
                            return wireManager.LaunchMissile(targetID, station);
                        }
                    };
                    Func<bool> canPress = () => station.HasFireControl;
                    button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface, canPress);
                    menu.AddButton(button, -1);

                    buttonPos.X += buttonSize.X + spacing;
                    getText = () => "FIRE ALL";
                    action = () =>
                    {
                        if (coordinator.NumSelectedBays == 0)
                        {
                            Menu missileMenu = CreateBayMenu(window.Center, window, true, true);
                            return window.OpenMenu(missileMenu);
                        }
                        else
                        {
                            return wireManager.LaunchMissiles(targetID, station);
                        }
                    };
                    canPress = () => station.HasFireControl;
                    button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface, canPress);
                    menu.AddButton(button, -1);

                    buttonPos.X += buttonSize.X + spacing;

                    getText = () => "SET REL";
                    action = () =>
                    {
                        Vector2 relationMenuPos = window.Pos + new Vector2(window.Size.X * 0.5f, window.Size.Y - 100f);
                        Menu relationMenu = CreateRelationMenu(relationMenuPos, targetID, window, true, true);
                        return window.OpenMenu(relationMenu);
                    };

                    button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button, -1);

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

                    Func<bool> autoClose = () => window.SelectedEntityID != targetID;

                    Menu menu = new Menu(window, menuPos, menuSize, 5f, autoClose: autoClose);
                    Vector2 buttonPos = menu.Pos + padding;
                    Func<string> getText = () => "VIEW";
                    Func<bool> action = () => true;
                    Button button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button, -1);

                    buttonPos.X += buttonSize.X + spacing;
                    getText = () => "FIRE MISL";
                    action = () =>
                    {
                        if (coordinator.NumSelectedBays == 0)
                        {
                            Menu missileMenu = CreateBayMenu(window.Center, window, true, true);
                            return window.OpenMenu(missileMenu);
                        }
                        else
                        {
                            return wireManager.LaunchMissile(targetID, station);
                        }
                    };
                    Func<bool> canPress = () => station.HasFireControl;
                    button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface, canPress);
                    menu.AddButton(button, -1);

                    buttonPos.X += buttonSize.X + spacing;
                    getText = () => "FIRE ALL";
                    action = () =>
                    {
                        if (coordinator.NumSelectedBays == 0)
                        {
                            Menu missileMenu = CreateBayMenu(window.Center, window, true, true);
                            return window.OpenMenu(missileMenu);
                        }
                        else
                        {
                            return wireManager.LaunchMissiles(targetID, station);
                        }
                    };
                    canPress = () => station.HasFireControl;
                    button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface, canPress);
                    menu.AddButton(button, -1);

                    buttonPos.X += buttonSize.X + spacing;

                    getText = () => "FORGET";
                    action = () => wireManager.ForgetTarget(entity.EntityID);

                    button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface);
                    menu.AddButton(button, -1);

                    return menu;
                }
            }

            public static Menu CreateBayMenu(Vector2 pos, TargetingWindow window, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;
                UIWireManager wireManager = window.UI.UIWireManager;
                ControlStation station = window.UI.Station;

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

                Func<bool> autoClose = () => !station.HasFireControl;
                Menu menu = new Menu(window, menuPos, menuSize, 5f, obscure: true, surface: surface, autoClose: autoClose);

                Vector2 labelPos = menu.Pos + new Vector2(menu.Size.X * 0.5f - labelSize.X * 0.5f, headerHeight * 0.5f - labelSize.Y * 0.5f);
                RectangleF labelBounds = new RectangleF(labelPos, labelSize);
                MySprite labelSprite = SpriteHelper.CreateText(labelBounds, "-MISSILE BAYS-\n------------------", Color.White, window.Display, TextAlignment.CENTER, true, 0);
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
                            InfoPanel panel = new InfoPanel(panelPos, panelSize, 5f, 10f, getText, surface);
                            menu.AddInfoPanel(panel, i);

                            Vector2 buttonPos = panel.Pos + new Vector2(panel.Size.X * 0.5f - selectButtonSize.X * 0.5f, panel.Size.Y - selectButtonSize.Y - 10f);
                            getText = () => bay.IsSelected ? "SELECTED" : "SELECT";
                            Func<bool> onPress = () => wireManager.SelectBay(bay, station);
                            Func<bool> onRelease = () => wireManager.DeselectBay(bay, station);
                            Func<bool> isPressed = () => bay.IsSelected;
                            Func<bool> canPress = () => bay.IsSelectable;
                            Func<bool> canRelease = canPress;
                            ToggleButton button = new ToggleButton(buttonPos, selectButtonSize, 7f, 3f, 1f, getText, onPress, onRelease, isPressed, surface, canPress: canPress, canRelease: canRelease);
                            menu.AddButton(button, i);
                            bayIndex++;
                        }
                    }
                }

                Vector2 confirmButtonSize = new Vector2(150f, 50f);
                Vector2 confirmButtonPos = menu.Pos + new Vector2(menu.Size.X * 0.5f - confirmButtonSize.X - 20f, menu.Size.Y - footerHeight * 0.5f - confirmButtonSize.Y * 0.5f);
                Func<string> confirmText = () => "SELECT ALL";
                Func<bool> action = () => wireManager.SelectAllBays(station);
                Button confirmButton = new Button(confirmButtonPos, confirmButtonSize, 10f, 4f, 1f, confirmText, action, surface);
                menu.AddButton(confirmButton, -1);
                Vector2 cancelButtonSize = new Vector2(150f, 50f);
                Vector2 cancelButtonPos = menu.Pos + new Vector2(menu.Size.X * 0.5f + 20f, menu.Size.Y - footerHeight * 0.5f - confirmButtonSize.Y * 0.5f);
                Func<string> cancelText = () => "CLEAR ALL";
                action = () => wireManager.ClearSelectedBays(station);
                Button cancelButton = new Button(cancelButtonPos, cancelButtonSize, 10f, 4f, 1f, cancelText, action, surface);
                menu.AddButton(cancelButton, -1);
                return menu;
            }

            public static Menu CreateRelationMenu(Vector2 pos, long targetID, TargetingWindow window, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;
                UIWireManager wireManager = window.UI.UIWireManager;

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

                Func<bool> autoClose = () => window.SelectedEntityID != targetID;

                Menu menu = new Menu(window, menuPos, menuSize, 5f, autoClose: autoClose);

                Vector2 buttonPos = menu.Pos + padding;
                Func<string> getText = () => "FRNDLY";
                Func<bool> action = () => wireManager.SetRelation(targetID, EntityRelation.Friendly);
                Button button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface);
                menu.AddButton(button, -1);

                buttonPos.X += buttonSize.X + spacing;
                getText = () => "NTRL";
                action = () => wireManager.SetRelation(targetID, EntityRelation.Neutral);
                button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface);
                menu.AddButton(button, -1);

                buttonPos.X += buttonSize.X + spacing;
                getText = () => "HSTL";
                action = () => wireManager.SetRelation(targetID, EntityRelation.Hostile);
                button = new Button(buttonPos, buttonSize, 5f, 3f, 1f, getText, action, surface);
                menu.AddButton(button, -1);

                return menu;
            }

            public static Menu CreateLaserControlMenu(Vector2 pos, Window window, bool vertCent = false, bool horCent = false)
            {
                IMyTextSurface surface = window.Display;
                UIWireManager wireManager = window.UI.UIWireManager;

                var lasers = wireManager.TargetingLasers;
                var ctrlStation = window.UI.Station;

                int numLasers = lasers.Count;
                int numColumns = 5;
                int numRows = 2;

                float padding = 20f;
                float spacing = 10f;

                float headerHeight = 100f;

                Vector2 labelSize = new Vector2(300f, 50f);
                Vector2 panelSize = new Vector2(150f, 175f);
                Vector2 ctrlButtonSize = new Vector2(100f, 35f);

                float totalWidth = panelSize.X * numColumns + spacing * (numColumns - 1) + 2 * padding;
                float totalHeight = headerHeight + panelSize.Y * numRows + spacing * (numRows - 1);

                Vector2 menuSize = new Vector2(totalWidth, totalHeight);
                Vector2 menuPos = pos;
                if (horCent) menuPos.X -= menuSize.X / 2f;
                if (vertCent) menuPos.Y -= menuSize.Y / 2f;

                Menu menu = new Menu(window, menuPos, menuSize, 5f, obscure: true, surface: surface);

                Vector2 labelPos = menu.Pos + new Vector2(menu.Size.X * 0.5f - labelSize.X * 0.5f, headerHeight * 0.5f - labelSize.Y * 0.5f);
                RectangleF labelBounds = new RectangleF(labelPos, labelSize);
                MySprite labelSprite = SpriteHelper.CreateText(labelBounds, "-TARGETING LASERS-\n------------------", Color.White, window.Display, TextAlignment.CENTER, true, 0);
                menu.AddSprite(labelSprite, -1);

                int numPages = (int)Math.Ceiling((float)numLasers / (numRows * numColumns));
                int laserIndex = 0;

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
                            if (laserIndex >= numLasers) break;
                            var laser = lasers[laserIndex];
                            panelPos.X = menu.Pos.X + padding + (panelSize.X + spacing) * k;
                            Func<string> getText = () => laser.ToString();
                            InfoPanel panel = new InfoPanel(panelPos, panelSize, 5f, 10f, getText, surface);
                            menu.AddInfoPanel(panel, i);

                            Vector2 buttonPos = panel.Pos + new Vector2(panel.Size.X * 0.5f - ctrlButtonSize.X * 0.5f, panel.Size.Y - ctrlButtonSize.Y - 10f);
                            getText = () => "CTRL";
                            Func<bool> action = () => ctrlStation.TakeControl(laser);
                            Func<bool> isPressed = () => ReferenceEquals(ctrlStation.Controllable, laser);
                            Func<bool> canPress = () => !laser.IsUnderControl;
                            Func<bool> canRelease = canPress;
                            Button button = new Button(buttonPos, ctrlButtonSize, 7f, 3f, 1f, getText, action, surface, canPress);
                            menu.AddButton(button, i);
                            laserIndex++;
                        }
                    }
                }
                return menu;
            }

            public static Window CreateMainWindow(UI ui, float borderThickness)
            {
                Window window = new Window(ui, borderThickness);

                Vector2 labelSize = new Vector2(250f, 100f);
                Vector2 labelPos = window.Pos;
                RectangleF labelBounds = new RectangleF(labelPos, labelSize);
                MySprite labelFillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = labelBounds.Center,
                    Size = labelBounds.Size - 2 * borderThickness,
                    Color = UIConfig.WindowFillColor,
                    Alignment = TextAlignment.CENTER
                };
                MySprite labelBorderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = labelBounds.Center,
                    Size = labelBounds.Size,
                    Color = UIConfig.WindowBorderColor,
                    Alignment = TextAlignment.CENTER
                };
                MySprite labelTextSprite = SpriteHelper.CreateText(labelBounds, "-MAIN-", Color.White, ui.Display, TextAlignment.CENTER, true, borderThickness + 10f);
                window.AddSprite(labelBorderSprite);
                window.AddSprite(labelFillSprite);
                window.AddSprite(labelTextSprite);

                float padding = 50f;
                float spacing = 35f;
                float headerHeight = 150f;
                Vector2 buttonSize = new Vector2(350f, 125f);
                Vector2 buttonPos = window.Pos + new Vector2(padding, headerHeight);

                Func<string> getText = () => "LASER CTRL";
                Func<bool> action = () => window.OpenMenu(CreateLaserControlMenu(window.Center, window, true, true));
                Button laserCtrlButton = new Button(buttonPos, buttonSize, 20f, 10f, 5f, getText, action, ui.Display);

                window.AddButton(laserCtrlButton);

                buttonPos.X += buttonSize.X + spacing;

                getText = () => "TARGETING";
                action = () => ui.OpenWindow(new TargetingWindow(ui, 5f));

                Button targetingButton = new Button(buttonPos, buttonSize, 20f, 10f, 5f, getText, action, ui.Display);
                window.AddButton(targetingButton);

                return window;
            }
        }
    }
}
