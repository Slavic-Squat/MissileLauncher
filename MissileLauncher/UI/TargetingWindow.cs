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
            public NavMode NavMode { get; private set; } = NavMode.UI;
            public EntityTypeFilter NavTypeFilter { get; private set; } = EntityTypeFilter.Targets;
            public EntityRelationFilter NavRelationFilter { get; private set; } = EntityRelationFilter.All;
            public EntitySourceFilter NavSourceFilter { get; private set; } = EntitySourceFilter.Both;
            public ScopeScale ScopeScale { get; private set; } = ScopeScale.Medium;
            public long SelectedEntityID { get; private set; }

            private IReadOnlyDictionary<long, EntityInfoExt> _allEntities;
            private IReadOnlyDictionary<long, MyEntitySprite> _entitySprites;
            private IReadOnlyList<MySpriteExt> _targetingSprites;
            private TargetingSpriteBuilder _targetingSpriteBuilder;


            public TargetingWindow(UI ui, Vector2 pos, Vector2 size, float borderThickness) : base(ui, pos, size, borderThickness, canUserClose: false)
            {
                Init();
            }

            public TargetingWindow(UI ui, float borderThickness) : base(ui, borderThickness, canUserClose: false)
            {
                Init();
            }

            protected override void BuildSprites()
            {
                _additionalSprites.Clear();
                base.BuildSprites();

                MySprite[] borderSprite = SpriteHelper.CreateBoxHollow(Bounds, UIConfig.WindowBorderColor, _borderThickness);
                _additionalSprites.AddRange(borderSprite);

                RectangleF labelBounds = new RectangleF(Pos.X, Pos.Y, 250f, 100f);
                MySprite[] labelBox = SpriteHelper.CreateBoxFilled(labelBounds, UIConfig.WindowBorderColor, UIConfig.WindowFillColor, _borderThickness);
                _additionalSprites.AddRange(labelBox);
                MySprite labelTextSprite = SpriteHelper.CreateText(labelBounds, "-TARGETING-", Color.White, alignment: TextAlignment.CENTER, vertCentered: true, padding: _borderThickness + 10f);
                _additionalSprites.Add(labelTextSprite);
            }

            private void Init()
            {
                _allEntities = UI.UICoordinator.AllEntities;

                _targetingSpriteBuilder = new TargetingSpriteBuilder(new RectangleF(0, 0, 1024f, 1024f));
                _targetingSpriteBuilder.Zoom = MiscEnumHelper.GetScopeScaleValue(ScopeScale);

                _entitySprites = _targetingSpriteBuilder.EntitySprites;
                _targetingSprites = _targetingSpriteBuilder.FinalSprites;

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
                InfoPanel targetPanel = new InfoPanel(targetInfoPanelPos, targetInfoPanelSize, 5f, 10f, targetInfoGetter);
                AddInfoPanel(targetPanel);

                Vector2 navFilterPanelPos = Pos + new Vector2(0, 100f);

                ControlPanel navFilterPanel = UIFactory.CreateTargetingNavFilterPanel(navFilterPanelPos, this);
                AddControlPanel(navFilterPanel);

                Vector2 actionsPanelPos = Pos + new Vector2(250f, 0);
                ControlPanel actionsPanel = UIFactory.CreateTargetingActionsPanel(actionsPanelPos, this);
                AddControlPanel(actionsPanel);

                Vector2 targetingInfoPanelSize = new Vector2(200, 200f);
                Vector2 targetingInfoPanelPos = Pos + new Vector2(Size.X - targetingInfoPanelSize.X, 0);

                MissileCoordinator coordinator = UI.UICoordinator.MissileCoordinator;
                AWACS awacs = UI.UICoordinator.AWACS;
                Func<string> targetingInfoGetter = () => coordinator.GetOverview() + (awacs == null ? "" : $"\n{awacs.GetOverview().TrimEnd(' ', '\n')}");
                InfoPanel targetingInfoPanel = new InfoPanel(targetingInfoPanelPos, targetingInfoPanelSize, 5f, 10f, targetingInfoGetter);
                AddInfoPanel(targetingInfoPanel);

                Vector2 navModeInfoPanelSize = new Vector2(180f, 35f);
                Vector2 navModeInfoPanelPos = Pos + new Vector2(0, Size.Y - navModeInfoPanelSize.Y);
                Func<string> navModeInfoGetter = () => $"NAV MODE: {MiscEnumHelper.GetNavModeStr(NavMode)}";
                InfoPanel navModeInfoPanel = new InfoPanel(navModeInfoPanelPos, navModeInfoPanelSize, 3f, 5f, navModeInfoGetter);
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
                frame.AddRange(_bodySprites);
                
                _targetingSpriteBuilder.BuildSprites(_allEntities, targetedID: SelectedEntityID);
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

                NavMode = MiscEnumHelper.NextNavMode(NavMode);
            }

            public void CycleScopeScale()
            {
                ScopeScale = MiscEnumHelper.NextScopeScale(ScopeScale);
                _targetingSpriteBuilder.Zoom = MiscEnumHelper.GetScopeScaleValue(ScopeScale);
            }

            public void CycleTypeFilter()
            {
                NavTypeFilter = EntityFilterEnumHelper.NextEntityTypeFilter(NavTypeFilter);
            }

            public void CycleRelationFilter()
            {
                NavRelationFilter = EntityFilterEnumHelper.NextEntityRelationFilter(NavRelationFilter);
            }

            public void CycleSourceFilter()
            {
                NavSourceFilter = EntityFilterEnumHelper.NextEntitySourceFilter(NavSourceFilter);
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

                Dictionary<long, MyEntitySprite> filtered = _entitySprites.Where(kvp => EntityFilterEnumHelper.Matches(kvp.Value.EntityInfo, NavTypeFilter, NavRelationFilter, NavSourceFilter)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (!filtered.Any())
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
