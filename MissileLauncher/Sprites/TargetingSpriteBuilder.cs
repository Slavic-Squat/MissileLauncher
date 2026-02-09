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
                    _projectionMatrix = MatrixD.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n, _f);
                    BuildStaticSprites();
                }
            }
            public IReadOnlyList<MySpriteExt> FinalSprites => _finalSprites;
            public IReadOnlyDictionary<long, MyEntitySprite> EntitySprites => _entitySprites;

            private float _FOV = 30;
            private float _AR = 1;
            private float _n = 100;
            private float _f = 100000;
            private float _zoom = 1f;
            private Vector3D _localCameraPos = new Vector3D(31334, 30557, 63764);

            private List<MySpriteExt> _spritesPrePlane = new List<MySpriteExt>();
            private List<MySpriteExt> _planeSprites = new List<MySpriteExt>();
            private List<MySpriteExt> _spritesPostPlane = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSpritesPostPlane = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSpritesPrePlane = new List<MySpriteExt>();
            private List<MySpriteExt> _finalSprites = new List<MySpriteExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();

            private MatrixD _projectionMatrix = MatrixD.Identity;
            private RectangleF _screenBounds;

            public TargetingSpriteBuilder(RectangleF screenBounds)
            {
                _screenBounds = screenBounds;
                _projectionMatrix = MatrixD.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n, _f);

                BuildStaticSprites();
                
            }

            private void BuildStaticSprites()
            {
                MatrixD cameraTargetWorld = SystemCoordinator.ReferenceWorldMatrix;
                Vector3D cameraPositionWorld = Vector3D.Transform(_localCameraPos, cameraTargetWorld);

                Vector3D targetToCamera = cameraPositionWorld - cameraTargetWorld.Translation;
                double targetToCameraDist = targetToCamera.Length();
                Vector3D targetToCameraDir = targetToCamera / targetToCameraDist;

                cameraPositionWorld = cameraTargetWorld.Translation + targetToCameraDir * (targetToCameraDist / _zoom);
                targetToCameraDist /= _zoom;

                MatrixD viewMatrix = MatrixD.CreateLookAt(cameraPositionWorld, cameraTargetWorld.Translation, cameraTargetWorld.Up);

                PlaneD gridPlaneWorld = new PlaneD(cameraTargetWorld.Translation, cameraTargetWorld.Up);

                Vector3D selfPosLocal = new Vector3D(0, 200, 0);
                Vector3D selfPosWorld = Vector3D.Transform(selfPosLocal, cameraTargetWorld);
                Vector3D selfPosView = Vector3D.Transform(selfPosWorld, viewMatrix);
                Vector4D selfPosClip = Vector4D.Transform(new Vector4D(selfPosView, 1), _projectionMatrix);
                Vector3 selfPosNDC = new Vector3(selfPosClip.X / selfPosClip.W, selfPosClip.Y / selfPosClip.W, selfPosClip.Z / selfPosClip.W);
                Vector2 selfPosPixel = new Vector2((1 + selfPosNDC.X) * _screenBounds.Width / 2f, (1 - selfPosNDC.Y) * _screenBounds.Height / 2f);

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

                Vector3D basePosWorld = selfPosWorld - (Vector3D.Dot(gridPlaneWorld.Normal, selfPosWorld) + gridPlaneWorld.D) * gridPlaneWorld.Normal;
                Vector3D basePosView = Vector3D.Transform(basePosWorld, viewMatrix);
                Vector4D basePosClip = Vector4D.Transform(new Vector4D(basePosView, 1), _projectionMatrix);
                Vector3 basePosNDC = new Vector3(basePosClip.X / basePosClip.W, basePosClip.Y / basePosClip.W, basePosClip.Z / basePosClip.W);
                Vector2 basePosPixel = new Vector2((1 + basePosNDC.X) * _screenBounds.Width / 2f, (1 - basePosNDC.Y) * _screenBounds.Height / 2f);
                float baseDepthScale = (float)(targetToCameraDist / -basePosView.Z);

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

                Vector3D stemPosWorld = 0.5f * (selfPosWorld + basePosWorld);
                Vector3D stemPosView = Vector3D.Transform(stemPosWorld, viewMatrix);
                Vector4D stemPosClip = Vector4D.Transform(new Vector4D(stemPosView, 1), _projectionMatrix);
                Vector3 stemPosNDC = new Vector3(stemPosClip.X / stemPosClip.W, stemPosClip.Y / stemPosClip.W, stemPosClip.Z / stemPosClip.W);
                Vector2 stemPosPixel = new Vector2((1 + stemPosNDC.X) * _screenBounds.Width / 2f, (1 - stemPosNDC.Y) * _screenBounds.Height / 2f);

                Vector2 stemVector = new Vector2(selfPosPixel.X - basePosPixel.X, selfPosPixel.Y - basePosPixel.Y);
                float stemLength = stemVector.Length();
                double stemAngle = Math.Atan2(stemVector.Y, stemVector.X);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = stemPosPixel,
                    Size = new Vector2(stemLength, 1),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = (float)stemAngle,
                };

                MySpriteExt stemSpriteExt = new MySpriteExt(tempSprite, stemPosNDC.Z);

                Vector3D cameraTargetView = Vector3D.Transform(cameraTargetWorld.Translation, viewMatrix);
                Vector4D cameraTargetClip = Vector4D.Transform(new Vector4D(cameraTargetView, 1), _projectionMatrix);
                Vector3 cameraTargetNDC = new Vector3(cameraTargetClip.X / cameraTargetClip.W, cameraTargetClip.Y / cameraTargetClip.W, cameraTargetClip.Z / cameraTargetClip.W);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grid_0",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
                    Color = new Color(128, 128, 128, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gridSpriteExt = new MySpriteExt(tempSprite, cameraTargetNDC.Z);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grad_0",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
                    Color = new Color(1, 89, 68, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gradSpriteExt = new MySpriteExt(tempSprite, cameraTargetNDC.Z);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "StarryBackground",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
                    Color = new Color(200, 200, 200, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgSpriteExt = new MySpriteExt(tempSprite, 0.99999f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
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

            public void BuildSprites(IReadOnlyDictionary<long, EntityInfoExt> entityInfoExts, long targetedID = -1)
            {
                _finalSprites.Clear();
                _entitySprites.Clear();

                _spritesPrePlane.Clear();
                _spritesPostPlane.Clear();

                MatrixD cameraTargetWorld = SystemCoordinator.ReferenceWorldMatrix;
                Vector3D cameraPositionWorld = Vector3D.Transform(_localCameraPos, cameraTargetWorld);

                Vector3D targetToCamera = cameraPositionWorld - cameraTargetWorld.Translation;
                double targetToCameraDist = targetToCamera.Length();
                Vector3D targetToCameraDir = targetToCamera / targetToCameraDist;

                cameraPositionWorld = cameraTargetWorld.Translation + targetToCameraDir * (targetToCameraDist / _zoom);
                targetToCameraDist /= _zoom;

                MatrixD viewMatrix = MatrixD.CreateLookAt(cameraPositionWorld, cameraTargetWorld.Translation, cameraTargetWorld.Up);

                PlaneD gridPlaneWorld = new PlaneD(cameraTargetWorld.Translation, cameraTargetWorld.Up);

                foreach (var entityInfoExtKVP in entityInfoExts)
                {
                    EntityInfoExt entityInfoExt = entityInfoExtKVP.Value;
                    long key = entityInfoExtKVP.Key;

                    double distance = Vector3D.Distance(cameraTargetWorld.Translation, entityInfoExt.Position);

                    if (distance > 12000f / _zoom)
                    {
                        continue;
                    }

                    EntityInfo entityInfo = entityInfoExt.Info;

                    Vector3D entityPosWorld = entityInfo.Position;
                    Vector3D entityPosView = Vector3D.Transform(entityPosWorld, viewMatrix);
                    Vector4D entityPosClip = Vector4D.Transform(new Vector4D(entityPosView, 1), _projectionMatrix);
                    Vector3 entityPosNDC = new Vector3(entityPosClip.X / entityPosClip.W, entityPosClip.Y / entityPosClip.W, entityPosClip.Z / entityPosClip.W);
                    Vector2 entityPosPixel = new Vector2((1 + entityPosNDC.X) * _screenBounds.Width / 2f, (1 - entityPosNDC.Y) * _screenBounds.Height / 2f);
                    float entityDepthScale = (float)(targetToCameraDist / -entityPosView.Z);

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

                    _entitySprites.Add(key, entitySprite);

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

                    Vector3D basePosWorld = entityPosWorld - (Vector3D.Dot(gridPlaneWorld.Normal, entityPosWorld) + gridPlaneWorld.D) * gridPlaneWorld.Normal;
                    Vector3D basePosView = Vector3D.Transform(basePosWorld, viewMatrix);
                    Vector4D basePosClip = Vector4D.Transform(new Vector4D(basePosView, 1), _projectionMatrix);
                    Vector3 basePosNDC = new Vector3(basePosClip.X / basePosClip.W, basePosClip.Y / basePosClip.W, basePosClip.Z / basePosClip.W);
                    Vector2 basePosPixel = new Vector2((1 + basePosNDC.X) * _screenBounds.Width / 2f, (1 - basePosNDC.Y) * _screenBounds.Height / 2f);
                    float baseDepthScale = (float)(targetToCameraDist / -basePosView.Z);

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

                    Vector3D stemPosWorld = 0.5f * (entityPosWorld + basePosWorld);
                    Vector3D stemPosView = Vector3D.Transform(stemPosWorld, viewMatrix);
                    Vector4D stemPosClip = Vector4D.Transform(new Vector4D(stemPosView, 1), _projectionMatrix);
                    Vector3 stemPosNDC = new Vector3(stemPosClip.X / stemPosClip.W, stemPosClip.Y / stemPosClip.W, stemPosClip.Z / stemPosClip.W);
                    Vector2 stemPosPixel = new Vector2((1 + stemPosNDC.X) * _screenBounds.Width / 2f, (1 - stemPosNDC.Y) * _screenBounds.Height / 2f);

                    Vector2 stemVector = new Vector2(entityPosPixel.X - basePosPixel.X, entityPosPixel.Y - basePosPixel.Y);
                    float stemLength = stemVector.Length();
                    double stemAngle = Math.Atan2(stemVector.Y, stemVector.X);

                    tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = stemPosPixel,
                        Size = new Vector2(stemLength, 1),
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = (float)stemAngle,
                    };

                    MySpriteExt stemSpriteExt = new MySpriteExt(tempSprite, stemPosNDC.Z);

                    if ((Vector3D.Dot(cameraPositionWorld, gridPlaneWorld.Normal) + gridPlaneWorld.D) * (Vector3D.Dot(entityPosWorld, gridPlaneWorld.Normal) + gridPlaneWorld.D) > 0)
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

                _finalSprites.AddRange(_spritesPrePlane.Concat(_staticSpritesPrePlane).OrderBy(x => -x.Depth));
                _finalSprites.AddRange(_planeSprites);
                _finalSprites.AddRange(_spritesPostPlane.Concat(_staticSpritesPostPlane).OrderBy(x => -x.Depth));
            }
        }
    }
}
