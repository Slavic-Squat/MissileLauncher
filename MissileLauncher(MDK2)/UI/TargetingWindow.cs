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
        public class TargetingWindow : Window
        {
            public NavMode NavMode { get; private set; } = NavMode.Targeting;
            public EntityTypeFilter NavTypeFilter { get; private set; } = EntityTypeFilter.Targets;
            public EntityRelationFilter NavRelationFilter { get; private set; } = EntityRelationFilter.All;
            public EntitySourceFilter NavSourceFilter { get; private set; } = EntitySourceFilter.Both;
            public ScopeScale ScopeScale { get; private set; } = ScopeScale.Close;
            public long SelectedEntityID { get; private set; }

            private Dictionary<long, EntityInfoExt> _allEntities = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();
            private List<MySpriteExt> _targetingSprites = new List<MySpriteExt>();

            private TargetingSpriteBuilder _targetingSpriteBuilder;


            public TargetingWindow(UI ui, Vector2 pos, Vector2 size, float borderThickness) : base(ui, pos, size, borderThickness)
            {
                Init();
            }

            public TargetingWindow(UI ui, float borderThickness) : base(ui, borderThickness)
            {
                Init();
            }

            protected override void BuildSprites()
            {
                _additionalSprites.Clear();
                base.BuildSprites();

                Vector2 leftBorderSize = new Vector2(_borderThickness, Size.Y);
                Vector2 leftBorderPos = Pos;
                RectangleF leftBorderBounds = new RectangleF(leftBorderPos, leftBorderSize);
                MySprite leftBorderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = leftBorderBounds.Center,
                    Size = leftBorderBounds.Size,
                    Color = UIConfig.WindowBorderColor,
                    Alignment = TextAlignment.CENTER
                };
                _additionalSprites.Add(leftBorderSprite);

                Vector2 rightBorderSize = new Vector2(_borderThickness, Size.Y);
                Vector2 rightBorderPos = Pos + new Vector2(Size.X - _borderThickness, 0);
                RectangleF rightBorderBounds = new RectangleF(rightBorderPos, rightBorderSize);
                MySprite rightBorderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = rightBorderBounds.Center,
                    Size = rightBorderBounds.Size,
                    Color = UIConfig.WindowBorderColor,
                    Alignment = TextAlignment.CENTER
                };
                _additionalSprites.Add(rightBorderSprite);

                Vector2 topBorderSize = new Vector2(Size.X, _borderThickness);
                Vector2 topBorderPos = Pos;
                RectangleF topBorderBounds = new RectangleF(topBorderPos, topBorderSize);
                MySprite topBorderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = topBorderBounds.Center,
                    Size = topBorderBounds.Size,
                    Color = UIConfig.WindowBorderColor,
                    Alignment = TextAlignment.CENTER
                };
                _additionalSprites.Add(topBorderSprite);

                Vector2 bottomBorderSize = new Vector2(Size.X, _borderThickness);
                Vector2 bottomBorderPos = Pos + new Vector2(0, Size.Y - _borderThickness);
                RectangleF bottomBorderBounds = new RectangleF(bottomBorderPos, bottomBorderSize);
                MySprite bottomBorderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = bottomBorderBounds.Center,
                    Size = bottomBorderBounds.Size,
                    Color = UIConfig.WindowBorderColor,
                    Alignment = TextAlignment.CENTER
                };
                _additionalSprites.Add(bottomBorderSprite);

                Vector2 labelSize = new Vector2(250f, 100f);
                Vector2 labelPos = Pos;
                RectangleF labelBounds = new RectangleF(labelPos, labelSize);
                MySprite labelFillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = labelBounds.Center,
                    Size = labelBounds.Size - 2 * _borderThickness,
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
                MySprite labelTextSprite = SpriteHelper.CreateText(labelBounds, "-TARGETING-", Color.White, Display, TextAlignment.CENTER, true, _borderThickness + 10f);
                AddSprite(labelBorderSprite);
                AddSprite(labelFillSprite);
                AddSprite(labelTextSprite);
            }

            private void Init()
            {
                _allEntities = UI.UICoordinator.AllEntities;

                _targetingSpriteBuilder = new TargetingSpriteBuilder();
                _targetingSpriteBuilder.Zoom = GetValue(ScopeScale);

                Vector2 targetInfoPanelSize = new Vector2(150, 200);
                Vector2 targetInfoPanelPos = Pos + new Vector2(Size.X - targetInfoPanelSize.X, Size.Y * 0.5f - targetInfoPanelSize.Y * 0.5f);
                Func<string> targetInfoGetter = () =>
                {
                    if (_entitySprites.Keys.Contains(SelectedEntityID))
                    {
                        return _entitySprites[SelectedEntityID].EntityInfo.ToString();
                    }
                    else
                    {
                        return "No Target Selected";
                    }
                };
                InfoPanel targetPanel = new InfoPanel(targetInfoPanelPos, targetInfoPanelSize, 5f, 10f, targetInfoGetter, Display);
                AddInfoPanel(targetPanel);

                Vector2 navFilterPanelPos = Pos + new Vector2(0, 100f);

                ControlPanel navFilterPanel = UIFactory.CreateTargetingNavFilterPanel(navFilterPanelPos, this);
                AddControlPanel(navFilterPanel);

                Vector2 actionsPanelPos = Pos + new Vector2(250f, 0);
                ControlPanel actionsPanel = UIFactory.CreateTargetingActionsPanel(actionsPanelPos, this);
                AddControlPanel(actionsPanel);

                Vector2 targetingInfoPanelSize = new Vector2(200, 130f);
                Vector2 targetingInfoPanelPos = Pos + new Vector2(Size.X - targetingInfoPanelSize.X, 0);

                MissileCoordinator coordinator = UI.UICoordinator.MissileCoordinator;
                AWACS awacs = UI.UICoordinator.AWACS;
                Func<string> targetingInfoGetter = () => coordinator.ToString() + $"\nAWACS TRGTS: {awacs.TargetCount}";
                InfoPanel targetingInfoPanel = new InfoPanel(targetingInfoPanelPos, targetingInfoPanelSize, 5f, 10f, targetingInfoGetter, Display);
                AddInfoPanel(targetingInfoPanel);

                Vector2 navModeInfoPanelSize = new Vector2(180f, 35f);
                Vector2 navModeInfoPanelPos = Pos + new Vector2(0, Size.Y - navModeInfoPanelSize.Y);
                Func<string> navModeInfoGetter = () => $"NAV MODE: {GetName(NavMode)}";
                InfoPanel navModeInfoPanel = new InfoPanel(navModeInfoPanelPos, navModeInfoPanelSize, 3f, 5f, navModeInfoGetter, Display);
                AddInfoPanel(navModeInfoPanel);
            }

            private void OpenEntityMenu(long entityID)
            {
                if (_allEntities.Keys.Contains(entityID))
                {
                    Vector2 menuPos = Pos + new Vector2(Size.X * 0.5f, Size.Y - 100f);
                    Menu menu = UIFactory.CreateEntityMenu(menuPos, entityID, this, true, true);
                    OpenMenu(menu);
                }
            }

            private void SelectEntity(long entityID)
            {
                SelectedEntityID = entityID;
            }

            private void UnselectEntity()
            {
                SelectedEntityID = -1;
            }

            public override void Update(double time)
            {
                if (!IsOpen) return;
                base.Update(time);

                if (!_allEntities.ContainsKey(SelectedEntityID))
                {
                    UnselectEntity();
                }
            }

            public override void Draw(MySpriteDrawFrame frame)
            {
                if (!IsOpen) return;
                BuildSprites();
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);

                _targetingSprites = _targetingSpriteBuilder.BuildSprites(_allEntities, out _entitySprites, targetedID: SelectedEntityID);
                foreach (var sprite in _targetingSprites)
                {
                    sprite.Draw(frame);
                }

                foreach (var sprite in _additionalSprites)
                {
                    frame.Add(sprite);
                }

                foreach (var element in _uiElements)
                {
                    element.Draw(frame);

                    if (ReferenceEquals(element, _navigatedElement) || ReferenceEquals(element, _highlightedElement))
                    {
                        continue;
                    }
                }

                if (_navigatedElement != null)
                {
                    _navigatedElement.Draw(frame);
                }
                else
                {
                    _highlightedElement?.Draw(frame);
                }
            }

            public void CycleNavMode()
            {
                switch (NavMode)
                {
                    case NavMode.UI:
                        UnhighlightElement(_highlightedElement);
                        break;
                    case NavMode.Targeting:
                        break;
                }

                NavMode = NextNavMode(NavMode);
            }

            public void CycleScopeScale()
            {
                ScopeScale = NextScopeScale(ScopeScale);
                _targetingSpriteBuilder.Zoom = GetValue(ScopeScale);
            }

            public void CycleTypeFilter()
            {
                NavTypeFilter = NextEntityTypeFilter(NavTypeFilter);
            }

            public void CycleRelationFilter()
            {
                NavRelationFilter = NextEntityRelationFilter(NavRelationFilter);
            }

            public void CycleSourceFilter()
            {
                NavSourceFilter = NextEntitySourceFilter(NavSourceFilter);
            }

            public override void Navigate(UserInput input, object caller)
            {
                if (!IsOpen || !IsNavigating || IsPaused || !ReferenceEquals(Parent, caller))
                {
                    return;
                }
                if (_navigatedElement != null)
                {
                    _navigatedElement.Navigate(input, this);
                    return;
                }

                if (input.QRelease)
                {
                    CycleNavMode();
                }

                switch (NavMode)
                {
                    case NavMode.UI:
                        NavigateUI(input, caller);
                        break;
                    case NavMode.Targeting:
                        NavigateTargeting(input);
                        break;
                }
            }

            private void NavigateUI(UserInput input, object caller)
            {
                base.Navigate(input, caller);
            }

            private void NavigateTargeting(UserInput input)
            {
                if (input.CRelease)
                {
                    Close();
                    return;
                }

                Dictionary<long, MyEntitySprite> filtered = _entitySprites.Where(kvp => Matches(kvp.Value.EntityInfo, NavTypeFilter, NavRelationFilter, NavSourceFilter)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (filtered.Count() == 0)
                {
                    UnselectEntity();
                    return;
                }
                else if (!filtered.Keys.Contains(SelectedEntityID))
                {
                    UnselectEntity();
                    long firstEntityID = filtered.Keys.OrderBy(id => filtered[id].EntityInfo.Position.X + filtered[id].EntityInfo.Position.Y).FirstOrDefault();
                    SelectEntity(firstEntityID);
                }

                if (input.WRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, SelectedEntityID, Direction.Up);
                    SelectEntity(nextEntityID);
                }
                else if (input.SRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, SelectedEntityID, Direction.Down);
                    SelectEntity(nextEntityID);
                }
                else if (input.ARelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, SelectedEntityID, Direction.Left);
                    SelectEntity(nextEntityID);
                }
                else if (input.DRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, SelectedEntityID, Direction.Right);
                    SelectEntity(nextEntityID);
                }
                else if (input.SpaceRelease)
                {
                    OpenEntityMenu(SelectedEntityID);
                }
            }
        }
    }
}
