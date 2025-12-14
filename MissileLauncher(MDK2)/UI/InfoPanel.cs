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
        public class InfoPanel : IPanel
        {
            public RectangleF Bounds => _bounds;
            public Vector2 Pos => _bounds.Position;
            public Vector2 Size => _bounds.Size;
            public Vector2 Center => _bounds.Center;

            private Func<string> _textGetter;

            private RectangleF _bounds;
            private float _borderThickness;
            private float _padding;

            private MySprite[] _bodySprites;
            private MySprite _textSprite;

            public InfoPanel(Vector2 pos, Vector2 size, float borderThickness, float padding, Func<string> textGetter)
            {
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _textGetter = textGetter;
                _padding = padding;
            }

            private void BuildSprites()
            {
                _bodySprites = SpriteHelper.CreateBoxFilled(Bounds, UIConfig.PanelBorderColor, UIConfig.PanelFillColor, _borderThickness);
                _textSprite = SpriteHelper.CreateText(Bounds, _textGetter(), Color.White, alignment: TextAlignment.LEFT, vertCentered: false, padding: _borderThickness + _padding);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                BuildSprites();
                frame.AddRange(_bodySprites);
                frame.Add(_textSprite);
            }
        }
    }
}
