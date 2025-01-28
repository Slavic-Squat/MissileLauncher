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
        public struct Sprite3D
        {
            public Sprite3DType Type3D { get; }
            public long EntityID { get; }
            public float Z { get; }

            #region Generic Fields
            private string _spriteName;
            private Color _color;
            private Vector3 _position;
            private Vector2 _size;
            private float _rotation;
            #endregion

            #region Vector Fields
            private Vector3 _origin;
            private Vector3 _endPoint;
            private Vector3 _midPoint;
            private float _length;
            #endregion

            public enum Sprite3DType
            {
                Missile, Target, Base, Launcher, Selector, Vector, Misc
            }

            private Sprite3D(Sprite3DType type3D, long entityID, string spriteName, Vector3 position, Vector2 size, float rotation, Color color)
            {
                Type3D = type3D;
                EntityID = entityID;
                _spriteName = spriteName;
                _position = position;
                Z = position.Z;
                _size = size;
                _rotation = rotation;
                _color = color;

                _origin = position;
                _endPoint = position;
                _midPoint = position;
                _length = 0;
            }

            private Sprite3D(Vector3 origin, Vector3 endPoint, Color color)
            {
                Type3D = Sprite3DType.Vector;
                EntityID = -1;
                _spriteName = "SquareSimple";
                _color = color;
                _origin = origin;
                _endPoint = endPoint;

                _midPoint = (_endPoint + _origin) / 2;
                var diff = _endPoint - _origin;
                _length = diff.Length();
                _rotation = (float)-Math.Atan2(diff.Y, diff.X);

                _position = _midPoint;
                Z = origin.Z;
                _size = new Vector2(_length, 0);

            }

            public static Sprite3D CreateSprite3D(Sprite3DType type, string spriteName, long entityID, Vector3 position, float ar, float scale, float rotation, Color color)
            {
                Vector2 size = new Vector2(1, 1 / ar) * scale;
                return new Sprite3D(type, entityID, spriteName, position, size, rotation, color);
            }

            public static Sprite3D CreateVectorSprite3D(Vector3 origin, Vector3 endPoint, Color color)
            {
                return new Sprite3D(origin, endPoint, color);
            }

            public bool IsEmpty()
            {
                return EntityID == 0;
            }

            public MySprite ToMySprite(Vector2 screenSize)
            {
                if (Type3D != Sprite3DType.Vector)
                {
                    Vector2 posScreen = new Vector2(_position.X, -_position.Y) * screenSize.X / 2 + screenSize / 2;
                    Vector2 sizeScreen = _size * screenSize.X;
                    return new MySprite(type: SpriteType.TEXTURE, data: _spriteName, position: posScreen, size: sizeScreen, color: _color, rotation: _rotation);
                }
                else
                {
                    Vector2 posScreen = new Vector2(_midPoint.X, -_midPoint.Y) * screenSize.X / 2 + screenSize / 2;
                    Vector2 sizeScreen = new Vector2(_length / 2 * screenSize.X, 1);
                    return new MySprite(type: SpriteType.TEXTURE, data: "SquareSimple", position: posScreen, size: sizeScreen, color: _color, rotation: _rotation);
                }
            }
        }
    }
}
