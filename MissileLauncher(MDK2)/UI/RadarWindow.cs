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
        public class RadarWindow : IWindow, IUpdatable
        {
            public UI UI { get; private set; }
            public Vector2 Pos { get; private set; }
            public Vector2 Size { get; private set; }
            public bool IsInside { get; private set; }

            private long SelfID => UI.UIWireManager.GetSelfID();
            private IMyCubeBlock ReferenceBlock => UI.UIWireManager.GetReferenceBlock();
            private Dictionary<long, EntityInfoExt> AllEntities => UI.UIWireManager.GetAllEntities();
            private Dictionary<long, MissileInfo> AllMyMissiles => UI.UIWireManager.GetAllMyMissiles();

            private TargetingSpriteBuilder _targetingSpriteBuilder;
            private Dictionary<long, DepthSprite> EntitySprites => _targetingSpriteBuilder.EntitySprites;

            private List<MySprite> _sprites = new List<MySprite>();

            private List<IHighlightable> _highlightableElements = new List<IHighlightable>();
            private List<IUpdatable> _updatableElements = new List<IUpdatable>();
            private List<IUIElement> _allElements = new List<IUIElement>();

            private IHighlightable _highlightedElement;
            private IEnterable _enteredElement;
            private long _targetedEntityID;

            private enum NavMode
            {
                UI, Targeting, MissileControl,
            }
            private NavMode _currentNavMode = NavMode.UI;
            private Dictionary<NavMode, string> _navModeDisplayNames = new Dictionary<NavMode, string>()
            {
                { NavMode.UI, "UI" },
                { NavMode.Targeting, "Targeting" },
                { NavMode.MissileControl, "Missile Control" },
            };

            private enum TargetingNavType
            {
                All, Targets, Missiles,
            }
            private Dictionary<TargetingNavType, string> _targetingNavTypeDisplayNames = new Dictionary<TargetingNavType, string>()
            {
                { TargetingNavType.All, "All" },
                { TargetingNavType.Targets, "Targets" },
                { TargetingNavType.Missiles, "Missiles" },
            };
            private Dictionary<TargetingNavType, Func<HashSet<long>>> _targetingNavTypeFilters;
            private TargetingNavType _currentNavType = TargetingNavType.All;

            private enum TargetingNavRelation
            {
                All, Hostile, Neutral, Friendly,
            }
            private Dictionary<TargetingNavRelation, string> _targetingNavRelationDisplayNames = new Dictionary<TargetingNavRelation, string>()
            {
                { TargetingNavRelation.All, "All" },
                { TargetingNavRelation.Hostile, "Hostile" },
                { TargetingNavRelation.Neutral, "Neutral" },
                { TargetingNavRelation.Friendly, "Friendly" },
            };
            private Dictionary<TargetingNavRelation, Func<HashSet<long>>> _targetingNavRelationFilters;
            private TargetingNavRelation _currentNavRelation = TargetingNavRelation.All;

            private enum ScopeScale
            {
                Close, Far
            }
            private Dictionary<ScopeScale, string> _scopeScaleDisplayNames = new Dictionary<ScopeScale, string>()
            {
                { ScopeScale.Close, "6Km" },
                { ScopeScale.Far, "12Km" },
            };
            private Dictionary<ScopeScale, int> _scopeScaleValues = new Dictionary<ScopeScale, int>()
            {
                { ScopeScale.Close, 2 },
                { ScopeScale.Far, 1 },
            };
            private ScopeScale _currentScopeScale = ScopeScale.Close;


            public RadarWindow(UI ui, Vector2 pos, Vector2 size)
            {
                UI = ui;
                Pos = pos;
                Size = size;
                
                Init();
            }

            public RadarWindow(UI ui)
            {
                UI = ui;
                Pos = new Vector2(ui.TextureSize.X / 2f, ui.TextureSize.Y / 2f);
                Size = new Vector2(ui.SurfaceSize.X, ui.SurfaceSize.Y);

                Init();
            }

            public void Init()
            {
                BuildSprites();

                _targetingSpriteBuilder = new TargetingSpriteBuilder(ReferenceBlock);

                _targetingNavTypeFilters = new Dictionary<TargetingNavType, Func<HashSet<long>>>()
                {
                    { TargetingNavType.All, () => AllEntities.Keys.ToHashSet() },
                    { TargetingNavType.Targets, () => AllEntities.Where(kvp => kvp.Value.EntityType == EntityInfoExt.Type.Target).Select(kvp => kvp.Key).ToHashSet() },
                    { TargetingNavType.Missiles, () => AllEntities.Where(kvp => kvp.Value.EntityType == EntityInfoExt.Type.Missile).Select(kvp => kvp.Key).ToHashSet() },
                };

                _targetingNavRelationFilters = new Dictionary<TargetingNavRelation, Func<HashSet<long>>>()
                {
                    { TargetingNavRelation.All, () => AllEntities.Keys.ToHashSet() },
                    { TargetingNavRelation.Hostile, () => AllEntities.Where(kvp => kvp.Value.EntityRelation == EntityInfoExt.Relation.Hostile).Select(kvp => kvp.Key).ToHashSet() },
                    { TargetingNavRelation.Neutral, () => AllEntities.Where(kvp => kvp.Value.EntityRelation == EntityInfoExt.Relation.Neutral).Select(kvp => kvp.Key).ToHashSet() },
                    { TargetingNavRelation.Friendly, () => AllEntities.Where(kvp => kvp.Value.EntityRelation == EntityInfoExt.Relation.Friendly).Select(kvp => kvp.Key).ToHashSet() },
                };
            }

            private void BuildSprites()
            {
                _sprites.Clear();
                MySprite backgroundSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos,
                    Size = Size,
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(backgroundSprite);
            }

            public void Enter()
            {
                if (_highlightableElements.Count > 0)
                {
                    HighlightElement(_highlightableElements[0]);
                }
                IsInside = true;
            }

            public void Exit()
            {
                IsInside = false;
                UnhighlightCurrentElement();
                ExitCurrentElement();
            }

            private void HighlightElement(IHighlightable highlightable)
            {
                UnhighlightCurrentElement();
                highlightable.Highlight();
                _highlightedElement = highlightable;
            }

            private void UnhighlightCurrentElement()
            {
                _highlightedElement?.Unhighlight();
                _highlightedElement = null;
            }

            private void ActivateHighlightedElement(DateTime time)
            {
                if (_highlightedElement is IButton)
                {
                    ((IButton)_highlightedElement).Press(time);
                }
                else if (_highlightedElement is IEnterable)
                {
                    EnterElement((IEnterable)_highlightedElement);
                }
            }

            private void TargetEntity(long entityID)
            {
                _targetedEntityID = entityID;
            }

            private void UntargetEntity()
            {
                _targetedEntityID = -1;
            }

            private void EnterElement(IEnterable enterable)
            {
                ExitCurrentElement();
                enterable.Enter();
                _enteredElement = enterable;
            }

            private void ExitCurrentElement()
            {
                if (_enteredElement != null)
                {
                    _enteredElement.Exit();
                    _enteredElement = null;
                }
            }

            private void CleanUp()
            {
                if (!_enteredElement?.IsInside ?? false)
                {
                    _enteredElement = null;
                }
            }

            public void Update(DateTime time)
            {
                CleanUp();

                foreach (var element in _updatableElements)
                {
                    if (element == _enteredElement)
                    {
                        continue;
                    }
                    element.Update(time);
                }

                if (_enteredElement is IUpdatable)
                {
                    ((IUpdatable)_enteredElement).Update(time);
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.AddRange(_sprites);

                _targetingSpriteBuilder.Zoom = _scopeScaleValues[_currentScopeScale];
                _targetingSpriteBuilder.BuildSprites(AllEntities, AllMyMissiles, _targetedEntityID);

                foreach (var depthSprite in _targetingSpriteBuilder.FinalSprites)
                {
                    depthSprite.Draw(frame);
                }

                foreach (var element in _allElements)
                {
                    if (element == _enteredElement || element == _highlightedElement)
                    {
                        continue;
                    }

                    element.Draw(frame);
                }
                _highlightedElement?.Draw(frame);
                _enteredElement?.Draw(frame);
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (_enteredElement is INavigable)
                {
                    ((INavigable)_enteredElement).Navigate(input, time);
                }
                if (_enteredElement != null)
                {
                    return;
                }

                if (input.QRelease)
                {
                    _currentNavMode = (NavMode)_currentNavMode.Next();
                }

                switch (_currentNavMode)
                {
                    case NavMode.UI:
                        NavigateUI(input, time);
                        break;
                    case NavMode.Targeting:
                        NavigateTargeting(input, time);
                        break;
                    case NavMode.MissileControl:
                        NavigateMissileControl(input, time);
                        break;
                }
            }

            private void NavigateUI(UserInput input, DateTime time)
            {
                if (_highlightableElements.Count == 0)
                {
                    return;
                }

                if (input.CRelease)
                {
                    Exit();
                }
                else if (input.WRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightableElements, _highlightedElement, UIUtilities.NavigationDirection.Up);
                    HighlightElement(nextElement);
                }
                else if (input.SRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightableElements, _highlightedElement, UIUtilities.NavigationDirection.Down);
                    HighlightElement(nextElement);
                }
                else if (input.ARelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightableElements, _highlightedElement, UIUtilities.NavigationDirection.Left);
                    HighlightElement(nextElement);
                }
                else if (input.DRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightableElements, _highlightedElement, UIUtilities.NavigationDirection.Right);
                    HighlightElement(nextElement);
                }
                else if (input.SpaceRelease)
                {
                    ActivateHighlightedElement(time);
                }
            }

            private void NavigateTargeting(UserInput input, DateTime time)
            {
                HashSet<long> typeFilter = _targetingNavTypeFilters[_currentNavType]();
                HashSet<long> relationFilter = _targetingNavRelationFilters[_currentNavRelation]();
                HashSet<long> combinedFilter = typeFilter.Intersect(relationFilter).ToHashSet();

                Dictionary<long, DepthSprite> navigableSprites = EntitySprites.Where(kvp => combinedFilter.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (navigableSprites.Count == 0)
                {
                    UntargetEntity();
                    return;
                }

                if (input.CRelease)
                {
                    Exit();
                }
                else if (input.WRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(navigableSprites, _targetedEntityID, UIUtilities.NavigationDirection.Up);
                    TargetEntity(nextEntityID);
                }
                else if (input.SRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(navigableSprites, _targetedEntityID, UIUtilities.NavigationDirection.Down);
                    TargetEntity(nextEntityID);
                }
                else if (input.ARelease)
                {
                    long nextEntityID = UIUtilities.Navigate(navigableSprites, _targetedEntityID, UIUtilities.NavigationDirection.Left);
                    TargetEntity(nextEntityID);
                }
                else if (input.DRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(navigableSprites, _targetedEntityID, UIUtilities.NavigationDirection.Right);
                    TargetEntity(nextEntityID);
                }
            }

            private void NavigateMissileControl(UserInput input, DateTime time)
            {
                Dictionary<long, DepthSprite> navigableSprites = EntitySprites.Where(kvp => AllMyMissiles.ContainsKey(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (navigableSprites.Count == 0)
                {
                    UntargetEntity();
                    return;
                }

                if (input.CRelease)
                {
                    Exit();
                }
                else if (input.WRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(navigableSprites, _targetedEntityID, UIUtilities.NavigationDirection.Up);
                    TargetEntity(nextEntityID);
                }
                else if (input.SRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(navigableSprites, _targetedEntityID, UIUtilities.NavigationDirection.Down);
                    TargetEntity(nextEntityID);
                }
                else if (input.ARelease)
                {
                    long nextEntityID = UIUtilities.Navigate(navigableSprites, _targetedEntityID, UIUtilities.NavigationDirection.Left);
                    TargetEntity(nextEntityID);
                }
                else if (input.DRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(navigableSprites, _targetedEntityID, UIUtilities.NavigationDirection.Right);
                    TargetEntity(nextEntityID);
                }
            }
        }
    }
}
