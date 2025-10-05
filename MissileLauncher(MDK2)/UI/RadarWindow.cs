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
        public class RadarWindow : IWindow
        {
            public UI UI { get; private set; }
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsOpen { get; private set; }
            public bool IsNavigating { get; private set; }
            public bool IsPaused { get; private set; }
            public event Action<IWindow> RequestClose;
            public event Action<INavigable> RequestStopNavigation;

            public IMyTextSurface Display => UI.Display;
            public NavMode NavMode { get; set; } = NavMode.UI;
            public EntityTypeFilter NavTypeFilter { get; set; } = EntityTypeFilter.All;
            public EntityRelationFilter NavRelationFilter { get; set; } = EntityRelationFilter.All;
            public EntitySourceFilter NavSourceFilter { get; set; } = EntitySourceFilter.Both;
            public ScopeScale ScopeScale { get; set; } = ScopeScale.Close;
            private IMyCubeBlock ReferenceBlock => UI.UIWireManager.ReferenceBlock;

            private Dictionary<long, EntityInfoExt> _allEntities = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();
            private List<MySpriteExt> _targetingSprites = new List<MySpriteExt>();

            private TargetingSpriteBuilder _targetingSpriteBuilder;            

            private RectangleF _bounds;

            private List<MySprite> _sprites = new List<MySprite>();

            private List<IHighlightable> _highlightables = new List<IHighlightable>();
            private IHighlightable _highlightedElement;
            private List<IUpdatable> _updateables = new List<IUpdatable>();
            private List<IUIElement> _uiElements = new List<IUIElement>();
            private List<INavigable> _navigables = new List<INavigable>();
            private INavigable _navigatedElement;

            private InfoPanel _targetPanel;
            private ControlPanel _optionsPanel;            

            private long _selectedEntityID;


            public RadarWindow(UI ui, Vector2 pos, Vector2 size)
            {
                UI = ui;

                _bounds = new RectangleF(pos, size);

                Init();
            }

            public RadarWindow(UI ui)
            {
                UI = ui;
                Vector2 pos = (ui.TextureSize - ui.SurfaceSize) * 0.5f;
                Vector2 size = new Vector2(ui.SurfaceSize.X, ui.SurfaceSize.Y);

                _bounds = new RectangleF(pos, size);

                Init();
            }

            public void Init()
            {
                _allEntities = UI.UIWireManager.GetAllEntities();

                BuildSprites();

                _targetingSpriteBuilder = new TargetingSpriteBuilder(ReferenceBlock);

                Vector2 targetPanelSize = new Vector2(150, 200);
                Vector2 targetPanelPos = Pos + new Vector2(Size.X - targetPanelSize.X, Size.Y * 0.5f - targetPanelSize.Y * 0.5f);
                Func<string> targetInfoGetter = () =>
                {
                    if (_entitySprites.Keys.Contains(_selectedEntityID))
                    {
                        return _entitySprites[_selectedEntityID].EntityInfo.ToString(ReferenceBlock.GetPosition(), UI.UIWireManager.SystemTime);
                    }
                    else
                    {
                        return "No Target Selected";
                    }
                };
                _targetPanel = new InfoPanel(targetPanelPos, targetPanelSize, targetInfoGetter, Display);

                Vector2 optionsPanelSize = new Vector2(150, 300);
                Vector2 optionsPanelPos = Pos + new Vector2(0, Size.Y * 0.5f - optionsPanelSize.Y * 0.5f);

                _optionsPanel = UIFactory.CreateTargetingOptionsPanel(this, optionsPanelPos, optionsPanelSize);
                _highlightables.Add(_optionsPanel);
            }

            private void BuildSprites()
            {
                _sprites.Clear();
                MySprite fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER
                };
                _sprites.Add(fillSprite);
            }

            public void Open()
            {
                IsOpen = true;
                StartNavigation();
            }

            private void Close()
            {
                RequestClose?.Invoke(this);
                OnClose();
            }

            public void OnClose()
            {
                IsOpen = false;
                StopNavigation();
            }

            public void StartNavigation()
            {
                IsNavigating = true;
                ResumeNavigation();
            }

            private void StopNavigation()
            {
                RequestStopNavigation?.Invoke(this);
                OnStopNavigation();
            }

            public void OnStopNavigation()
            {
                IsNavigating = false;
                PauseNavigation();
            }

            public void ResumeNavigation()
            {
                IsPaused = false;
                if (_highlightables.Count > 0)
                {
                    HighlightElement(_highlightables[0]);
                }
            }

            public void PauseNavigation()
            {
                IsPaused = true;
                UnhighlightElement(_highlightedElement);
            }

            private void HighlightElement(IHighlightable highlightable)
            {
                UnhighlightElement(_highlightedElement);
                highlightable.Highlight();
                _highlightedElement = highlightable;
            }

            private void UnhighlightElement(IHighlightable hightlightable)
            {
                hightlightable?.Unhighlight();
                
                if (_highlightedElement == hightlightable)
                {
                    _highlightedElement = null;
                }
            }

            private void ActivateHighlightable(IHighlightable highlightable, DateTime time)
            {
                if (highlightable is IButton)
                {
                    ((IButton)highlightable).Press(time);
                }
                else if (highlightable is INavigable)
                {
                    NavigateElement((INavigable)highlightable);
                }
            }

            public void NavigateElement(INavigable navigable)
            {
                StopNavigatingElement(_navigatedElement);
                _navigables.Add(navigable);
                _navigatedElement = navigable;
                navigable.StartNavigation();
                navigable.RequestStopNavigation += StopNavigatingElement;
            }

            public void StopNavigatingElement(INavigable navigable)
            {
                if (ReferenceEquals(navigable, _navigatedElement))
                {
                    _navigatedElement = null;
                }
                _navigables.Remove(navigable);
                navigable.OnStopNavigation();
                navigable.RequestStopNavigation -= StopNavigatingElement;
            }

            public void OpenMenu(IMenu menu)
            {
                _updateables.Add(menu);
                _uiElements.Add(menu);

                NavigateElement(menu);

                menu.Open();
                menu.RequestClose += CloseMenu;
            }

            public void CloseMenu(IMenu menu)
            {
                _updateables.Remove(menu);
                _uiElements.Remove(menu);

                StopNavigatingElement(menu);
                menu.RequestClose -= CloseMenu;
                menu.OnClose();
            }

            private void OpenEntityMenu(long entityID)
            {
                if (_allEntities.Keys.Contains(entityID))
                {
                    EntityInfoExt entity = _allEntities[entityID];
                    Vector2 menuSize = new Vector2(500, 100);
                    Vector2 menuPos = Pos + new Vector2(Size.X * 0.5f - menuSize.X * 0.5f, Size.Y - menuSize.Y);
                    Menu menu = UIFactory.CreateEntityMenu(this, menuPos, menuSize, entity, UI.UIWireManager);
                    OpenMenu(menu);
                }
            }

            private void SelectEntity(long entityID)
            {
                _selectedEntityID = entityID;
            }

            private void UnselectEntity()
            {
                _selectedEntityID = -1;
            }

            public void Update(DateTime time)
            {
                _allEntities = UI.UIWireManager.GetAllEntities();

                _targetingSpriteBuilder.Zoom = GetValue(ScopeScale);
                _targetingSprites = _targetingSpriteBuilder.BuildSprites(_allEntities, _selectedEntityID, out _entitySprites);

                _optionsPanel.Update(time);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.AddRange(_sprites);

                foreach (var sprite in _targetingSprites)
                {
                    sprite.Draw(frame);
                }

                _targetPanel.Draw(frame);
                _optionsPanel.Draw(frame);
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (input.CRelease)
                {
                    Close();
                }

                if (input.QRelease)
                {
                    NavMode = NextNavMode(NavMode);
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
                if (_highlightables.Count == 0)
                {
                    return;
                }

                if (input.WRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, NavigationDirection.Up);
                    HighlightElement(nextElement);
                }
                else if (input.SRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, NavigationDirection.Down);
                    HighlightElement(nextElement);
                }
                else if (input.ARelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, NavigationDirection.Left);
                    HighlightElement(nextElement);
                }
                else if (input.DRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, NavigationDirection.Right);
                    HighlightElement(nextElement);
                }
                else if (input.SpaceRelease)
                {
                    ActivateHighlightable(_highlightedElement, time);
                }
            }

            private void NavigateTargeting(UserInput input, DateTime time)
            {
                Dictionary<long, MyEntitySprite> filtered = _entitySprites.Where(kvp => Matches(kvp.Value.EntityInfo, NavTypeFilter, NavRelationFilter, NavSourceFilter)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (filtered.Count() == 0)
                {
                    UnselectEntity();
                    return;
                }
                else if (!filtered.Keys.Contains(_selectedEntityID))
                {
                    UnselectEntity();
                }

                else if (input.WRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, _selectedEntityID, NavigationDirection.Up);
                    SelectEntity(nextEntityID);
                }
                else if (input.SRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, _selectedEntityID, NavigationDirection.Down);
                    SelectEntity(nextEntityID);
                }
                else if (input.ARelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, _selectedEntityID, NavigationDirection.Left);
                    SelectEntity(nextEntityID);
                }
                else if (input.DRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, _selectedEntityID, NavigationDirection.Right);
                    SelectEntity(nextEntityID);
                }
                else if (input.SpaceRelease)
                {
                    OpenEntityMenu(_selectedEntityID);
                }
            }
        }
    }
}
