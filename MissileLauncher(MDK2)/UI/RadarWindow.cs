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
            public long SelectedEntityID { get; private set; }

            private Dictionary<long, EntityInfoExt> _allEntities = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();
            private List<MySpriteExt> _targetingSprites = new List<MySpriteExt>();

            private TargetingSpriteBuilder _targetingSpriteBuilder;            

            private RectangleF _bounds;
            private float _borderThickness;

            private List<MySprite> _sprites = new List<MySprite>();

            private List<IHighlightable> _highlightables = new List<IHighlightable>();
            private IHighlightable _highlightedElement;
            private List<IUpdatable> _updateables = new List<IUpdatable>();
            private List<IUIElement> _uiElements = new List<IUIElement>();
            private List<INavigable> _navigables = new List<INavigable>();
            private INavigable _navigatedElement;


            public RadarWindow(UI ui, Vector2 pos, Vector2 size, float borderThickness)
            {
                UI = ui;

                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;

                Init();
            }

            public RadarWindow(UI ui, float borderThickness)
            {
                UI = ui;
                Vector2 pos = (ui.TextureSize - ui.SurfaceSize) * 0.5f;
                Vector2 size = new Vector2(ui.SurfaceSize.X, ui.SurfaceSize.Y);

                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;

                Init();
            }

            public void Init()
            {
                _allEntities = UI.UIWireManager.GetAllEntities();

                BuildSprites();

                _targetingSpriteBuilder = new TargetingSpriteBuilder();

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
                InfoPanel targetPanel = new InfoPanel(targetPanelPos, targetPanelSize, 5f, targetInfoGetter, Display);
                _uiElements.Add(targetPanel);
                _updateables.Add(targetPanel);

                Vector2 optionsPanelPos = Pos + new Vector2(0, Size.Y * 0.5f);

                ControlPanel optionsPanel = UIFactory.CreateTargetingOptionsPanel(optionsPanelPos, this, true);
                _highlightables.Add(optionsPanel);
                _uiElements.Add(optionsPanel);
                _updateables.Add(optionsPanel);
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

            public void OnOpen()
            {
                IsOpen = true;
            }

            private void Close()
            {
                RequestClose?.Invoke(this);
            }

            public void OnClose()
            {
                IsOpen = false;
            }

            public void OnStartNavigation()
            {
                IsNavigating = true;
                ResumeNavigation();
            }

            private void StopNavigation()
            {
                RequestStopNavigation?.Invoke(this);
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
                if (highlightable == null || ReferenceEquals(highlightable, _highlightedElement))
                {
                    return;
                }
                UnhighlightElement(_highlightedElement);
                highlightable.Highlight();
                _highlightedElement = highlightable;
            }

            private void UnhighlightElement(IHighlightable hightlightable)
            {
                if (hightlightable == null)
                {
                    return;
                }
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
                if (navigable == null || ReferenceEquals(navigable, _navigatedElement))
                {
                    return;
                }
                StopNavigatingElement(_navigatedElement);
                _navigables.Add(navigable);
                _navigatedElement = navigable;
                navigable.OnStartNavigation();
                navigable.RequestStopNavigation += StopNavigatingElement;
            }

            public void StopNavigatingElement(INavigable navigable)
            {
                if (navigable == null)
                {
                    return;
                }
                if (ReferenceEquals(navigable, _navigatedElement))
                {
                    _navigatedElement = null;
                }
                _navigables.Remove(navigable);
                navigable.OnStopNavigation();
                navigable.RequestStopNavigation -= StopNavigatingElement;

                NavigateElement(_navigables.LastOrDefault());
            }

            public void OpenMenu(IMenu menu)
            {
                _updateables.Add(menu);
                _uiElements.Add(menu);

                NavigateElement(menu);

                menu.OnOpen();
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
                    Vector2 menuPos = Pos + new Vector2(Size.X * 0.5f, Size.Y - 100f);
                    Menu menu = UIFactory.CreateEntityMenu(menuPos, SelectedEntityID, this, UI.UIWireManager, false, true);
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

            public void Update(DateTime time)
            {
                _allEntities = UI.UIWireManager.GetAllEntities();

                _targetingSpriteBuilder.Zoom = GetValue(ScopeScale);
                _targetingSprites = _targetingSpriteBuilder.BuildSprites(_allEntities, SelectedEntityID, out _entitySprites);

                if (!_allEntities.ContainsKey(SelectedEntityID))
                {
                    UnselectEntity();
                }

                foreach (var updatable in _updateables.ToList())
                {
                    updatable.Update(time);
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.AddRange(_sprites);

                foreach (var sprite in _targetingSprites)
                {
                    sprite.Draw(frame);
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

            public void Navigate(UserInput input, DateTime time)
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
                if (input.CRelease)
                {
                    Close();
                }

                if (_highlightables.Count == 0)
                {
                    return;
                }

                if (input.WRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, Direction.Up);
                    HighlightElement(nextElement);
                }
                else if (input.SRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, Direction.Down);
                    HighlightElement(nextElement);
                }
                else if (input.ARelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, Direction.Left);
                    HighlightElement(nextElement);
                }
                else if (input.DRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, Direction.Right);
                    HighlightElement(nextElement);
                }
                else if (input.SpaceRelease)
                {
                    ActivateHighlightable(_highlightedElement, time);
                }
            }

            private void NavigateTargeting(UserInput input, DateTime time)
            {
                if (input.CRelease)
                {
                    Close();
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
