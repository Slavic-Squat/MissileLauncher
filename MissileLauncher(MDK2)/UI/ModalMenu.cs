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
        public class ModalMenu : Menu, IModal
        {
            public bool CanClose => _canClose?.Invoke() ?? true;

            private Func<bool> _canClose;
            private bool _obscure;

            private IMyTextSurface _surface;
            private MySprite _obscureSprite;

            public ModalMenu(Vector2 pos, Vector2 size, float borderThickness, Func<bool> canClose, IMyTextSurface surface, bool obscure = true, Func<bool> autoClose = null) : base(pos, size, borderThickness, autoClose)
            {
                _canClose = canClose;
                _surface = surface;
                _obscure = obscure;
            }

            protected override void BuildSprites()
            {
                base.BuildSprites();
                _obscureSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = _surface.TextureSize * 0.5f,
                    Size = _surface.SurfaceSize,
                    RotationOrScale = 0f,
                    Color = new Color(0, 0, 0, 200),
                    Alignment = TextAlignment.CENTER,
                };
            }

            protected override void Close()
            {
                if (CanClose == false)
                {
                    return;
                }
                base.Close();
            }

            public override void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                if (_obscure)
                {
                    frame.Add(_obscureSprite);
                }
                frame.Add(_borderSprite);
                frame.Add(_fillSprite);

                foreach (var sprite in _commonSprites)
                {
                    frame.Add(sprite);
                }

                foreach (var uiElement in _commonUIElements)
                {
                    uiElement.Draw(frame);
                }

                if (_pages.Count > 0 && _pages.Count > _currentPageIndex)
                {
                    var currentPage = _pages[_currentPageIndex];
                    currentPage.Sprites.ForEach(s => frame.Add(s));
                    currentPage.UIElements.ForEach(e => e.Draw(frame));
                }
            }
        }
    }
}
