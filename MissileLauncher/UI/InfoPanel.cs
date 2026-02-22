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

            private List<MySprite> _bodySprites = new List<MySprite>();
            private MySprite _textSprite;

            private StringBuilder _sb = new StringBuilder();
            private IMyTextSurface _surface;

            public InfoPanel(IMyTextSurface surface, Vector2 pos, Vector2 size, float borderThickness, float padding, Func<string> textGetter)
            {
                _surface = surface;
                _bounds = new RectangleF(pos, size);
                _borderThickness = borderThickness;
                _textGetter = textGetter;
                _padding = padding;
            }

            private void BuildSprites()
            {
                _bodySprites.Clear();
                SpriteHelper.CreateBoxFilled(_bodySprites, _bounds, UIConfig.PanelBorderColor, UIConfig.PanelFillColor, _borderThickness);

                string text = _textGetter();
                _sb.Clear();
                _sb.Append(text);
                _textSprite = SpriteHelper.CreateText(_bounds.Position + (_borderThickness + _padding), _sb, Color.White, _surface, text: text, maxHeight: _bounds.Height - 2f * (_borderThickness + _padding), maxWidth: _bounds.Width - 2f * (_borderThickness + _padding), fontID: "Monospace");
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
