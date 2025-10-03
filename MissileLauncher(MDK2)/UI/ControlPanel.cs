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
        public class ControlPanel : IPanel, IEnterable, INavigable, IUpdatable, IHighlightable
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;
            public bool IsInside { get; private set; }
            public bool IsHighlighted { get; private set; }


            private RectangleF _bounds;
            private List<IButton> _buttons = new List<IButton>();
            private IButton _highlightedButton;

            private IMyTextSurface _surface;

            private MySprite _fillSprite;
            private MySprite _borderSprite;
            private MySprite _highlightSprite;

            private List<MySprite> _sprites = new List<MySprite>();

            public ControlPanel(Vector2 pos, Vector2 size, IMyTextSurface surface)
            {
                _bounds = new RectangleF(pos, size);
                IsInside = false;
                _surface = surface;

                BuildSprites();
            }

            public void BuildSprites()
            {
                float minDim = Math.Min(_bounds.Width, _bounds.Height);

                _fillSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size - 20f,
                    RotationOrScale = 0f,
                    Color = UIConfig.PanelFillColor,
                    Alignment = TextAlignment.CENTER,
                };
                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size,
                    RotationOrScale = 0f,
                    Color = UIConfig.PanelBorderColor,
                    Alignment = TextAlignment.CENTER,
                };
                _highlightSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Center,
                    Size = Size + 20f,
                    Color = UIConfig.PanelHighlightColor,
                    Alignment = TextAlignment.CENTER
                };
            }

            public void AddButton(IButton button)
            {
                _buttons.Add(button);
            }

            public void AddSprite(MySprite sprite)
            {
                _sprites.Add(sprite);
            }

            public void Highlight()
            {
                IsHighlighted = true;
            }

            public void Unhighlight()
            {
                IsHighlighted = false;
            }

            public void Enter()
            {
                IsInside = true;
            }

            public void Exit()
            {
                IsInside = false;
            }

            private void HighlightButton(IButton button)
            {
                UnhighlightButton(_highlightedButton);
                button.Highlight();
                _highlightedButton = button;
            }

            private void UnhighlightButton(IButton button)
            {
                button?.Unhighlight();

                if (_highlightedButton == button)
                {
                    _highlightedButton = null;
                }
            }

            public void Update(DateTime time)
            {
                foreach (var button in _buttons)
                {
                    button.Update(time);
                }
            }

            private void ActivateButton(IButton button, DateTime time)
            {
                button?.Press(time);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                if (IsHighlighted)
                {
                    frame.Add(_highlightSprite);
                }
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);

                foreach (var sprite in _sprites)
                {
                    frame.Add(sprite);
                }

                foreach (var button in _buttons)
                {
                    if (button == _highlightedButton)
                    {
                        continue;
                    }
                    button.Draw(frame);
                }
                _highlightedButton?.Draw(frame);
            }

            public void Navigate(UserInput input, DateTime time)
            {
                if (input.CRelease)
                {
                    Exit();
                }

                if (_buttons.Count == 0)
                {
                    return;
                }

                if (input.WRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, NavigationDirection.Up);
                    HighlightButton(nextButton);
                }
                else if (input.SRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, NavigationDirection.Down);
                    HighlightButton(nextButton);
                }
                else if (input.ARelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, NavigationDirection.Left);
                    HighlightButton(nextButton);
                }
                else if (input.DRelease)
                {
                    IButton nextButton = UIUtilities.Navigate(_buttons, _highlightedButton, NavigationDirection.Right);
                    HighlightButton(nextButton);
                }
                else if (input.SpaceRelease)
                {
                    ActivateButton(_highlightedButton, time);
                }
            }
        }
    }
}
