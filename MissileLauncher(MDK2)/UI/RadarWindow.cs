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
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsInside { get; private set; }

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
            private Stack<IMenu> _menuStack = new Stack<IMenu>();
            private TextPanel _targetPanel;
            private ControlPanel _optionsPanel;

            private IHighlightable _highlightedElement;
            private IEnterable _enteredElement;

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

                Vector2 targetPanelSize = new Vector2(200, 300);
                Vector2 targetPanelPos = Pos + new Vector2(Size.X - targetPanelSize.X, Size.Y - targetPanelSize.Y);
                _targetPanel = new TextPanel(targetPanelPos, targetPanelSize, "", Display);

                Vector2 optionsPanelSize = new Vector2(200, 300);
                Vector2 optionsPanelPos = Pos;

                _optionsPanel = UIFactory.CreateTargetingOptionsPanel(this, optionsPanelPos, optionsPanelSize, Display);
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

            public void Enter()
            {
                if (_highlightables.Count > 0)
                {
                    HighlightElement(_highlightables[0]);
                }
                IsInside = true;
            }

            public void Exit()
            {
                IsInside = false;
                UnhighlightElement(_highlightedElement);
                ExitElement(_enteredElement);
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
                else if (highlightable is IEnterable)
                {
                    EnterElement((IEnterable)highlightable);
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

            private void EnterElement(IEnterable enterable)
            {
                ExitElement(_enteredElement);
                enterable?.Enter();
                _enteredElement = enterable;
            }

            private void ExitElement(IEnterable enterable)
            {
                enterable?.Exit();
                if (_enteredElement == enterable)
                {
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
                _allEntities = UI.UIWireManager.GetAllEntities();

                _targetingSpriteBuilder.Zoom = GetValue(ScopeScale);
                _targetingSprites = _targetingSpriteBuilder.BuildSprites(_allEntities, _selectedEntityID, out _entitySprites);

                if (_entitySprites.Keys.Contains(_selectedEntityID))
                {
                    _targetPanel.Text = _entitySprites[_selectedEntityID].EntityInfo.ToString(ReferenceBlock.GetPosition(), time);
                }
                else
                {
                    UnselectEntity();
                    _targetPanel.Text = "No Target Selected";
                }

                _optionsPanel.Update(time);

                CleanUp();
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
                if (_enteredElement is INavigable)
                {
                    ((INavigable)_enteredElement).Navigate(input, time);
                }
                if (_enteredElement != null)
                {
                    return;
                }

                if (input.CRelease)
                {
                    Exit();
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

                if (input.CRelease)
                {
                    Exit();
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
            }
        }
    }
}
