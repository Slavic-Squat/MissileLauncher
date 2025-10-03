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

            private long SelfID => UI.UIWireManager.SelfID;
            private IMyCubeBlock ReferenceBlock => UI.UIWireManager.ReferenceBlock;

            private Dictionary<long, EntityInfoExt> _allEntities = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();
            private List<MySpriteExt> _targetingSprites = new List<MySpriteExt>();

            private TargetingSpriteBuilder _targetingSpriteBuilder;            

            private RectangleF _bounds;

            private List<MySprite> _sprites = new List<MySprite>();

            private List<IHighlightable> _highlightables = new List<IHighlightable>();
            private TextPanel _targetPanel;

            private IHighlightable _highlightedElement;
            private IEnterable _enteredElement;

            private long _selectedEntityID;
            
            private NavMode _navMode = NavMode.UI;
            private EntityTypeFilter _navTypeFilter = EntityTypeFilter.All;
            private EntityRelationFilter _navRelationFilter = EntityRelationFilter.All;
            private EntitySourceFilter _navSourceFilter = EntitySourceFilter.Both;
            private ScopeScale _scopeScale = ScopeScale.Close;


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
                _allEntities = UI.UIWireManager.GetAllEntities();

                _targetingSpriteBuilder.Zoom = GetValue(_scopeScale);
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

                CleanUp();

                if (_enteredElement is IUpdatable)
                {
                    ((IUpdatable)_enteredElement).Update(time);
                }
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.AddRange(_sprites);

                foreach (var depthSprite in _targetingSprites)
                {
                    depthSprite.Draw(frame);
                }

                _targetPanel.Draw(frame);

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

                if (input.CRelease)
                {
                    Exit();
                }

                if (input.QRelease)
                {
                    _navMode = NextNavMode(_navMode);
                }

                switch (_navMode)
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
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, UIUtilities.NavigationDirection.Up);
                    HighlightElement(nextElement);
                }
                else if (input.SRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, UIUtilities.NavigationDirection.Down);
                    HighlightElement(nextElement);
                }
                else if (input.ARelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, UIUtilities.NavigationDirection.Left);
                    HighlightElement(nextElement);
                }
                else if (input.DRelease)
                {
                    IHighlightable nextElement = UIUtilities.Navigate(_highlightables, _highlightedElement, UIUtilities.NavigationDirection.Right);
                    HighlightElement(nextElement);
                }
                else if (input.SpaceRelease)
                {
                    ActivateHighlightedElement(time);
                }
            }

            private void NavigateTargeting(UserInput input, DateTime time)
            {
                Dictionary<long, MyEntitySprite> filtered = _entitySprites.Where(kvp => Matches(kvp.Value.EntityInfo, _navTypeFilter, _navRelationFilter, _navSourceFilter)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (filtered.Count() == 0)
                {
                    UnselectEntity();
                    return;
                }

                if (input.CRelease)
                {
                    Exit();
                }
                else if (input.WRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, _selectedEntityID, UIUtilities.NavigationDirection.Up);
                    SelectEntity(nextEntityID);
                }
                else if (input.SRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, _selectedEntityID, UIUtilities.NavigationDirection.Down);
                    SelectEntity(nextEntityID);
                }
                else if (input.ARelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, _selectedEntityID, UIUtilities.NavigationDirection.Left);
                    SelectEntity(nextEntityID);
                }
                else if (input.DRelease)
                {
                    long nextEntityID = UIUtilities.Navigate(filtered, _selectedEntityID, UIUtilities.NavigationDirection.Right);
                    SelectEntity(nextEntityID);
                }
            }
        }
    }
}
