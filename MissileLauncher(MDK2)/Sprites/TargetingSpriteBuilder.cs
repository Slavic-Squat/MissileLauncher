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
        public class TargetingSpriteBuilder
        {
            public float Zoom
            {
                get { return _zoom; }
                set
                {
                    if (value == _zoom) return;
                    _zoom = value;
                    _n = 100 / _zoom;
                    _f = 100000 / _zoom;
                    _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n, _f);
                    BuildStaticSprites();
                }
            }

            private float _FOV = 30;
            private float _AR = 1;
            private float _n = 100;
            private float _f = 100000;
            private float _minScale = 0.8f;
            private float _maxScale = 1.2f;
            private float _zoom = 1f;
            private Vector3 _localCameraPos = new Vector3(31334, 30557, 63764);

            private List<MySpriteExt> _spritesPrePlane = new List<MySpriteExt>();
            private List<MySpriteExt> _planeSprites = new List<MySpriteExt>();
            private List<MySpriteExt> _spritesPostPlane = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSpritesPostPlane = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSpritesPrePlane = new List<MySpriteExt>();

            private Matrix _projectionMatrix = Matrix.Identity;

            public TargetingSpriteBuilder()
            {
                _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n, _f);

                BuildStaticSprites();
            }

            private void BuildStaticSprites()
            {
                Matrix cameraTargetWorld = SystemCoordinator.ReferenceWorldMatrix;
                Vector3 cameraPositionWorld = Vector3.Transform(_localCameraPos, cameraTargetWorld);

                Vector3 TargetToCamera = cameraPositionWorld - cameraTargetWorld.Translation;
                float TargetToCameraDist = TargetToCamera.Length();
                Vector3 TargetToCameraDir = TargetToCamera / TargetToCameraDist;

                cameraPositionWorld = cameraTargetWorld.Translation + TargetToCameraDir * (TargetToCameraDist / _zoom);

                Matrix viewMatrix = Matrix.CreateLookAt(cameraPositionWorld, cameraTargetWorld.Translation, cameraTargetWorld.Up);
                Matrix totalMatrix = viewMatrix * _projectionMatrix;

                Plane gridPlaneWorld = new Plane(cameraTargetWorld.Translation, cameraTargetWorld.Up);

                Vector3 selfPosLocal = new Vector3(0, 200, 0);
                Vector3 selfPosWorld = Vector3.Transform(selfPosLocal, cameraTargetWorld);
                Vector3 selfPosNDC = Vector3.Transform(selfPosWorld, totalMatrix);
                Vector2 selfPosPixel = new Vector2((1 + selfPosNDC.X) * 511, (1 - selfPosNDC.Y) * 511);

                MySprite tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Self_0",
                    Position = selfPosPixel,
                    Size = new Vector2(128, 128),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt selfSpriteExt = new MySpriteExt(tempSprite, selfPosNDC.Z);

                Vector3 basePosWorld = selfPosWorld - (Vector3.Dot(gridPlaneWorld.Normal, selfPosWorld) + gridPlaneWorld.D) * gridPlaneWorld.Normal;
                Vector3 basePosNDC = Vector3.Transform(basePosWorld, totalMatrix);
                Vector2 basePosPixel = new Vector2((1 + basePosNDC.X) * 511, (1 - basePosNDC.Y) * 511);
                float basePosZView = -(_f * _n) / (_f - basePosNDC.Z * (_f - _n));
                float baseDepthScale = _maxScale + (_minScale - _maxScale) * (-basePosZView - _n) / (_f - _n);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Base_0",
                    Position = basePosPixel,
                    Size = new Vector2(32, 32) * baseDepthScale,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt baseSpriteExt = new MySpriteExt(tempSprite, basePosNDC.Z);

                Vector3 stemPosWorld = 0.5f * (selfPosWorld + basePosWorld);
                Vector3 stemPosNDC = Vector3.Transform(stemPosWorld, totalMatrix);
                Vector2 stemPosPixel = new Vector2((1 + stemPosNDC.X) * 511, (1 - stemPosNDC.Y) * 511);

                Vector2 stemVector = new Vector2(selfPosPixel.X - basePosPixel.X, selfPosPixel.Y - basePosPixel.Y);
                float stemLength = stemVector.Length();
                float stemAngle = (float)Math.Atan2(stemVector.Y, stemVector.X);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = stemPosPixel,
                    Size = new Vector2(stemLength, 1),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = stemAngle,
                };

                MySpriteExt stemSpriteExt = new MySpriteExt(tempSprite, stemPosNDC.Z);

                Vector3 cameraTargetNDC = Vector3.Transform(cameraTargetWorld.Translation, totalMatrix);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grid_0",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = new Color(128, 128, 128, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gridSpriteExt = new MySpriteExt(tempSprite, cameraTargetNDC.Z);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grad_0",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = new Color(1, 89, 68, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gradSpriteExt = new MySpriteExt(tempSprite, cameraTargetNDC.Z);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "StarryBackground",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = new Color(200, 200, 200, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgSpriteExt = new MySpriteExt(tempSprite, 0.99999f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgFillSpriteExt = new MySpriteExt(tempSprite, 1f);

                _staticSpritesPrePlane.Clear();
                _planeSprites.Clear();
                _staticSpritesPostPlane.Clear();
                _staticSpritesPrePlane.Add(bgSpriteExt);
                _staticSpritesPrePlane.Add(bgFillSpriteExt);
                _planeSprites.Add(gridSpriteExt);
                _planeSprites.Add(gradSpriteExt);                
                _staticSpritesPostPlane.Add(selfSpriteExt);
                _staticSpritesPostPlane.Add(baseSpriteExt);
                _staticSpritesPostPlane.Add(stemSpriteExt);
            }

            public List<MySpriteExt> BuildSprites(IReadOnlyDictionary<long, EntityInfoExt> entityInfoExts, out Dictionary<long, MyEntitySprite> entitySprites, long targetedID = -1)
            {
                List<MySpriteExt> finalSprites = new List<MySpriteExt>();
                entitySprites = new Dictionary<long, MyEntitySprite>();

                _spritesPrePlane.Clear();
                _spritesPostPlane.Clear();

                Matrix cameraTargetWorld = SystemCoordinator.ReferenceWorldMatrix;
                Vector3 cameraPositionWorld = Vector3.Transform(_localCameraPos, cameraTargetWorld);

                Vector3 TargetToCamera = cameraPositionWorld - cameraTargetWorld.Translation;
                float TargetToCameraDist = TargetToCamera.Length();
                Vector3 TargetToCameraDir = TargetToCamera / TargetToCameraDist;

                cameraPositionWorld = cameraTargetWorld.Translation + TargetToCameraDir * (TargetToCameraDist / _zoom);

                Matrix viewMatrix = Matrix.CreateLookAt(cameraPositionWorld, cameraTargetWorld.Translation, cameraTargetWorld.Up);
                Matrix totalMatrix = viewMatrix * _projectionMatrix;

                Plane gridPlaneWorld = new Plane(cameraTargetWorld.Translation, cameraTargetWorld.Up);

                foreach (var entityInfoExtKVP in entityInfoExts)
                {
                    EntityInfoExt entityInfoExt = entityInfoExtKVP.Value;
                    long key = entityInfoExtKVP.Key;

                    float distance = Vector3.Distance(cameraTargetWorld.Translation, entityInfoExt.Position);

                    if (distance > 12000f / _zoom)
                    {
                        continue;
                    }

                    EntityInfo entityInfo = entityInfoExt.Info;

                    Vector3 entityPosWorld = entityInfo.Position;
                    Vector3 entityPosNDC = Vector3.Transform(entityPosWorld, totalMatrix);
                    Vector2 entityPosPixel = new Vector2((1 + entityPosNDC.X) * 511, (1 - entityPosNDC.Y) * 511);
                    float entityPosZView = -(_f * _n) / (_f - entityPosNDC.Z * (_f - _n));

                    float entityDepthScale = _maxScale + (_minScale - _maxScale) * (-entityPosZView - _n) / (_f - _n);

                    string spriteName = default(string);
                    Vector2 spriteSize = default(Vector2);
                    Color spriteColor = default(Color);

                    switch (entityInfoExt.Relation)
                    {
                        case EntityRelation.Me:
                            spriteColor = UIConfig.MeColor;
                            break;
                        case EntityRelation.Neutral:
                            spriteColor = UIConfig.NeutralColor;
                            break;
                        case EntityRelation.Friendly:
                            spriteColor = UIConfig.FriendlyColor;
                            break;
                        case EntityRelation.Hostile:
                            spriteColor = UIConfig.HostileColor;
                            break;
                        default:
                            spriteColor = Color.White;
                            break;
                    }                    

                    if (entityInfoExt.Type == EntityType.Missile)
                    {
                        spriteName = "Missile_0";
                        spriteSize = new Vector2(16, 16);
                    }
                    else
                    {
                        switch (entityInfoExt.Source)
                        {
                            case EntitySource.Local:
                                spriteName = "Target_0";
                                spriteSize = new Vector2(32, 32);
                                break;
                            case EntitySource.Remote:
                                spriteName = "Target_1";
                                spriteSize = new Vector2(32, 32);
                                break;
                            case EntitySource.Both:
                                spriteName = "Target_2";
                                spriteSize = new Vector2(32, 32);
                                break;
                            default:
                                spriteName = "Target_0";
                                spriteSize = new Vector2(32, 32);
                                break;
                        }
                    }

                    MySprite tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = spriteName,
                        Position = entityPosPixel,
                        Size = spriteSize * entityDepthScale,
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    MySpriteExt MySpriteExtEntity = new MySpriteExt(tempSprite, entityPosNDC.Z);
                    MyEntitySprite entitySprite = new MyEntitySprite(entityInfoExt, MySpriteExtEntity);

                    entitySprites.Add(key, entitySprite);

                    MySpriteExt selectorSpriteExt = default(MySpriteExt);

                    if (entityInfo.EntityID == targetedID)
                    {
                        tempSprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "Selector_0",
                            Position = entityPosPixel,
                            Size = MySpriteExtEntity.Sprite.Size * 1.5f,
                            Color = UIConfig.SelectorColor,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = 0f,
                        };

                        selectorSpriteExt = new MySpriteExt(tempSprite, entityPosNDC.Z - 0.001f);
                    }

                    Vector3 basePosWorld = entityPosWorld - (Vector3.Dot(gridPlaneWorld.Normal, entityPosWorld) + gridPlaneWorld.D) * gridPlaneWorld.Normal;
                    Vector3 basePosNDC = Vector3.Transform(basePosWorld, totalMatrix);
                    Vector2 basePosPixel = new Vector2((1 + basePosNDC.X) * 511, (1 - basePosNDC.Y) * 511);
                    float basePosZView = -(_f * _n) / (_f - basePosNDC.Z * (_f - _n));

                    float baseDepthScale = _maxScale + (_minScale - _maxScale) * (-basePosZView - _n) / (_f + _n);

                    tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Base_0",
                        Position = basePosPixel,
                        Size = spriteSize * baseDepthScale,
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    MySpriteExt baseSpriteExt = new MySpriteExt(tempSprite, basePosNDC.Z);

                    Vector3 stemPosWorld = 0.5f * (entityPosWorld + basePosWorld);
                    Vector3 stemPosNDC = Vector3.Transform(stemPosWorld, totalMatrix);
                    Vector2 stemPosPixel = new Vector2((1 + stemPosNDC.X) * 511, (1 - stemPosNDC.Y) * 511);

                    Vector2 stemVector = new Vector2(entityPosPixel.X - basePosPixel.X, entityPosPixel.Y - basePosPixel.Y);
                    float stemLength = stemVector.Length();
                    float stemAngle = (float)Math.Atan2(stemVector.Y, stemVector.X);

                    tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = stemPosPixel,
                        Size = new Vector2(stemLength, 1),
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = stemAngle,
                    };

                    MySpriteExt stemSpriteExt = new MySpriteExt(tempSprite, stemPosNDC.Z);

                    if ((Vector3.Dot(cameraPositionWorld, gridPlaneWorld.Normal) + gridPlaneWorld.D) * (Vector3.Dot(entityPosWorld, gridPlaneWorld.Normal) + gridPlaneWorld.D) > 0)
                    {
                        _spritesPostPlane.Add(MySpriteExtEntity);
                        _spritesPostPlane.Add(baseSpriteExt);
                        _spritesPostPlane.Add(stemSpriteExt);

                        if (selectorSpriteExt.IsValid)
                        {
                            _spritesPostPlane.Add(selectorSpriteExt);
                        }
                    }
                    else
                    {
                        _spritesPrePlane.Add(MySpriteExtEntity);
                        _spritesPrePlane.Add(baseSpriteExt);
                        _spritesPrePlane.Add(stemSpriteExt);

                        if (selectorSpriteExt.IsValid)
                        {
                            _spritesPrePlane.Add(selectorSpriteExt);
                        }
                    }
                }

                finalSprites.AddRange(_spritesPrePlane.Concat(_staticSpritesPrePlane).OrderBy(x => -x.Depth));
                finalSprites.AddRange(_planeSprites);
                finalSprites.AddRange(_spritesPostPlane.Concat(_staticSpritesPostPlane).OrderBy(x => -x.Depth));

                return finalSprites;
            }
        }
    }
}
