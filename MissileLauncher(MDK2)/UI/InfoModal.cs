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
        public class InfoModal : IModal
        {
            public Vector2 Pos { get; private set; }
            public Vector2 Size { get; private set; }
            public bool CanClose => _condition.Invoke();

            private Func<bool> _condition;
            private MySprite _backgroundSprite;
            private MySprite _borderSprite;
            private MySprite _textSprite;

            public InfoModal(Vector2 pos, Vector2 size, Func<bool> condition, string text, float textScale)
            {
                Pos = pos;
                Size = size;
                _condition = condition;

                _backgroundSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos,
                    Size = Size,
                    Color = new Color(0, 0, 0, 200),
                    Alignment = TextAlignment.CENTER
                };

                _borderSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = Pos,
                    Size = Size + new Vector2(20, 20),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER
                };

                _textSprite = SpriteHelper.CreateText(Pos, text, Color.White, textScale);
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.Add(_borderSprite);
                frame.Add(_backgroundSprite);
                frame.Add(_textSprite);
            }
        }
    }
}
