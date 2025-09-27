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
            public Vector2 Pos { get; private set; }
            public Vector2 Size { get; private set; }
            public bool IsInside { get; private set; }

            private UIWireManager _uiWireManager;
            private TargetingSpriteBuilder _targetingSpriteBuilder;
            private MySprite _sprite;
            private List<IHighlightable> _highlightableElements = new List<IHighlightable>();
            private List<IUpdatable> _updatableElements = new List<IUpdatable>();
            private List<IUIElement> _allElements = new List<IUIElement>();
            private IHighlightable _highlightedElement;
            private IEnterable _enteredElement;


            public RadarWindow(UI ui, Vector2 pos, Vector2 size, UIWireManager uiWireManager)
            {
                UI = ui;
                Pos = pos;
                Size = size;
                _uiWireManager = uiWireManager;

                _targetingSpriteBuilder = new TargetingSpriteBuilder(_uiWireManager.GetReferenceBlock(), _uiWireManager.GetAllEntities(), _uiWireManager.GetNeutralIDs(), _uiWireManager.GetFriendlyIDs(), _uiWireManager.GetHostileIDs(), _uiWireManager.GetLocalIDs(), _uiWireManager.GetRemoteIDs(), _uiWireManager.GetReferenceBlock().CubeGrid.EntityId);

                _sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = pos,
                    Size = size,
                    Color = UIConfig.WindowBackgroundColor,
                    Alignment = TextAlignment.CENTER
                };

                Button laserButton = new Button("LASER CTRL", pos + new Vector2(-250, 0), new Vector2(400, 100), "LASER CTRL", 2.0f, () => { return true; });
                Button radarButton = new Button("RADAR", pos + new Vector2(250, 0), new Vector2(400, 100), "LASER CTRL", 2.0f, () => { return true; });

                _highlightableElements.Add(laserButton);
                _highlightableElements.Add(radarButton);

                _updatableElements.Add(laserButton);
                _updatableElements.Add(radarButton);

                _allElements.Add(laserButton);
                _allElements.Add(radarButton);
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
                    CleanUpCurrentElement();
                }
            }

            private void CleanUpCurrentElement()
            {
                _enteredElement = null;
            }

            public void Update(DateTime time)
            {
                if (_enteredElement?.IsInside == false)
                {
                    CleanUpCurrentElement();
                }

                foreach (var element in _updatableElements)
                {
                    if (element == _enteredElement)
                    {
                        continue;
                    }
                    element.Update(time);
                }

                _enteredElement?.Update(time);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.Add(_sprite);

                _targetingSpriteBuilder.BuildSprites();

                foreach (var depthSprite in _targetingSpriteBuilder.FinalSprites)
                {
                    frame.Add(depthSprite.Sprite);
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
                if (_enteredElement != null)
                {
                    _enteredElement.Navigate(input, time);
                }
                else if (input.CHeldAndReleased)
                {
                    Exit();
                }
                else if (_highlightedElement == null)
                {
                    if (_highlightableElements.Count > 0)
                    {
                        HighlightElement(_highlightableElements[0]);
                    }
                    else
                    {
                        return;
                    }
                }
                else if (input.WRelease)
                {
                    IHighlightable nextElement = _highlightableElements.Where(element => element.Pos.Y < _highlightedElement.Pos.Y).OrderBy(element =>
                    {
                        float dx = Math.Abs(element.Pos.X - _highlightedElement.Pos.X);
                        float dy = Math.Abs(element.Pos.Y - _highlightedElement.Pos.Y);
                        return dx * 10 + dy;
                    }).FirstOrDefault() ?? _highlightedElement;

                    HighlightElement(nextElement);
                }
                else if (input.SRelease)
                {
                    IHighlightable nextElement = _highlightableElements.Where(element => element.Pos.Y > _highlightedElement.Pos.Y).OrderBy(element =>
                    {
                        float dx = Math.Abs(element.Pos.X - _highlightedElement.Pos.X);
                        float dy = Math.Abs(element.Pos.Y - _highlightedElement.Pos.Y);
                        return dx * 10 + dy;
                    }).FirstOrDefault() ?? _highlightedElement;

                    HighlightElement(nextElement);
                }
                else if (input.ARelease)
                {
                    IHighlightable nextElement = _highlightableElements.Where(element => element.Pos.X < _highlightedElement.Pos.X).OrderBy(element =>
                    {
                        float dx = Math.Abs(element.Pos.X - _highlightedElement.Pos.X);
                        float dy = Math.Abs(element.Pos.Y - _highlightedElement.Pos.Y);
                        return dx + dy * 10;
                    }).FirstOrDefault() ?? _highlightedElement;

                    HighlightElement(nextElement);
                }
                else if (input.DRelease)
                {
                    IHighlightable nextElement = _highlightableElements.Where(element => element.Pos.X > _highlightedElement.Pos.X).OrderBy(element =>
                    {
                        float dx = Math.Abs(element.Pos.X - _highlightedElement.Pos.X);
                        float dy = Math.Abs(element.Pos.Y - _highlightedElement.Pos.Y);
                        return dx + dy * 10;
                    }).FirstOrDefault() ?? _highlightedElement;

                    HighlightElement(nextElement);
                }
                else if (input.SpaceRelease)
                {
                    ActivateHighlightedElement(time);
                }
            }
        }
    }
}
