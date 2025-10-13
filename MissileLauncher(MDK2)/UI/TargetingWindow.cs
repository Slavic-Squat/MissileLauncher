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
            public EntityTypeFilter NavTypeFilter { get; private set; } = EntityTypeFilter.All;
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

            public void Init()
            {
                _allEntities = UI.UIWireManager.GetAllEntities();

                _targetingSpriteBuilder = new TargetingSpriteBuilder();
                _targetingSpriteBuilder.Zoom = GetValue(ScopeScale);

                Vector2 targetPanelSize = new Vector2(150, 200);
                Vector2 targetPanelPos = Pos + new Vector2(Size.X - targetPanelSize.X, Size.Y * 0.5f - targetPanelSize.Y * 0.5f);
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
                InfoPanel targetPanel = new InfoPanel(targetPanelPos, targetPanelSize, 5f, 10f, targetInfoGetter, Display);
                AddInfoPanel(targetPanel);

                Vector2 optionsPanelPos = Pos + new Vector2(0, Size.Y * 0.5f);

                ControlPanel optionsPanel = UIFactory.CreateTargetingOptionsPanel(optionsPanelPos, this, true);
                AddControlPanel(optionsPanel);
            }

            private void OpenEntityMenu(long entityID)
            {
                if (_allEntities.Keys.Contains(entityID))
                {
                    Vector2 menuPos = Pos + new Vector2(Size.X * 0.5f, Size.Y - 100f);
                    Menu menu = UIFactory.CreateEntityMenu(menuPos, entityID, this, false, true);
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

            public override void Update(DateTime time)
            {
                base.Update(time);

                _allEntities = UI.UIWireManager.GetAllEntities();

                if (!_allEntities.ContainsKey(SelectedEntityID))
                {
                    UnselectEntity();
                }
            }

            public override void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);

                _targetingSprites = _targetingSpriteBuilder.BuildSprites(_allEntities, SelectedEntityID, out _entitySprites);
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
                        PauseNavigation();
                        break;
                    case NavMode.Targeting:
                        ResumeNavigation();
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

            public override void Navigate(UserInput input, DateTime time)
            {
                if (_navigatedElement != null)
                {
                    _navigatedElement.Navigate(input, time);
                    return;
                }

                if (input.QRelease)
                {
                    CycleNavMode();
                }

                switch (NavMode)
                {
                    case NavMode.UI:
                        NavigateUI(input, time);
                        break;
                    case NavMode.Targeting:
                        NavigateTargeting(input, time);
                        break;
                }
            }

            private void NavigateUI(UserInput input, DateTime time)
            {
                base.Navigate(input, time);
            }

            private void NavigateTargeting(UserInput input, DateTime time)
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
