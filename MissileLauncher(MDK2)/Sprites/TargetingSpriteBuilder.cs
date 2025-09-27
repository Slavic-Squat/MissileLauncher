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
            #region Properties
            public Dictionary<long, DepthSprite> EntitySprites { get; private set; }
            public List<DepthSprite> FinalSprites { get; private set; }
            public float Zoom
            {
                get { return _zoom; }
                set
                {
                    _zoom = value;
                    BuildStaticSprites();
                }
            }
            #endregion

            #region Parts
            private IMyCubeBlock _referenceBlock;
            #endregion

            #region State Info
            #endregion

            #region Fields
            private float _FOV = 30;
            private float _AR = 1;
            private float _n = 100;
            private float _f = 100000;
            private float _minScale = 0.5f;
            private float _maxScale = 1.5f;
            private float _zoom = 1f;

            private Dictionary<long, EntityInfo> _entities;
            private HashSet<long> _neutralIDs;
            private HashSet<long> _friendlyIDs;
            private HashSet<long> _hostileIDs;
            private HashSet<long> _localIDs;
            private HashSet<long> _remoteIDs;
            private long _selfID = -1;

            private List<DepthSprite> _spritesBeforePlane = new List<DepthSprite>();
            private List<DepthSprite> _planeSprites = new List<DepthSprite>();
            private List<DepthSprite> _spritesAfterPlane = new List<DepthSprite>();
            private List<DepthSprite> _staticSpritesAfterPlane = new List<DepthSprite>();

            private Matrix _projectionMatrix = Matrix.Identity;
            #endregion

            public TargetingSpriteBuilder(IMyCubeBlock referenceBlock, Dictionary<long, EntityInfo> entities, HashSet<long> neutralIDs, HashSet<long> friendlyIDs, HashSet<long> hostileIDs, HashSet<long> localIDs, HashSet<long> remoteIDs, long selfID)
            {
                _referenceBlock = referenceBlock;
                _entities = entities;
                _neutralIDs = neutralIDs;
                _friendlyIDs = friendlyIDs;
                _hostileIDs = hostileIDs;
                _localIDs = localIDs;
                _remoteIDs = remoteIDs;
                _selfID = selfID;

                _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n, _f);
                EntitySprites = new Dictionary<long, DepthSprite>();
                FinalSprites = new List<DepthSprite>();

                BuildStaticSprites();
            }

            private void BuildStaticSprites()
            {
                Matrix cameraTargetWorld = _referenceBlock.WorldMatrix;
                Vector3 cameraPositionLocal = new Vector3(31334, 30557, 63764);
                Vector3 cameraPositionWorld = Vector3.Transform(cameraPositionLocal, cameraTargetWorld);

                Vector3 TargetToCamera = cameraPositionWorld - cameraTargetWorld.Translation;
                float TargetToCameraDist = TargetToCamera.Length();
                Vector3 TargetToCameraDir = TargetToCamera / TargetToCameraDist;

                cameraPositionWorld = cameraTargetWorld.Translation + TargetToCameraDir * (TargetToCameraDist / _zoom);

                Matrix viewMatrix = Matrix.CreateLookAt(cameraPositionWorld, cameraTargetWorld.Translation, cameraTargetWorld.Up);
                Matrix totalMatrix = viewMatrix * _projectionMatrix;

                Plane gridPlaneWorld = new Plane(cameraTargetWorld.Translation, cameraTargetWorld.Up);

                Vector3 selfPosLocal = new Vector3(0, 767, 0);
                Vector3 selfPosWorld = Vector3.Transform(selfPosLocal, cameraTargetWorld);
                Vector3 selfPosNDC = Vector3.Transform(selfPosWorld, totalMatrix);
                Vector2 selfPosPixel = new Vector2((1 + selfPosNDC.X) * 511, (1 - selfPosNDC.Y) * 511);

                MySprite selfSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Self_0",
                    Position = selfPosPixel,
                    Size = new Vector2(128, 128),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                DepthSprite selfDepthSprite = new DepthSprite(selfSprite, selfPosNDC.Z);

                Vector3 basePosWorld = selfPosWorld - (Vector3.Dot(gridPlaneWorld.Normal, selfPosWorld) + gridPlaneWorld.D) * gridPlaneWorld.Normal;
                Vector3 basePosNDC = Vector3.Transform(basePosWorld, totalMatrix);
                Vector2 basePosPixel = new Vector2((1 + basePosNDC.X) * 511, (1 - basePosNDC.Y) * 511);
                float basePosZView = -(_f * _n) / (_f - basePosNDC.Z * (_f - _n));
                float baseDepthScale = _minScale + (_maxScale - _minScale) * (-basePosZView - _n) / (_f - _n);

                MySprite baseSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Base_0",
                    Position = basePosPixel,
                    Size = new Vector2(32, 32) * baseDepthScale,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                DepthSprite baseDepthSprite = new DepthSprite(baseSprite, basePosNDC.Z);

                Vector3 stemPosWorld = 0.5f * (selfPosWorld + basePosWorld);
                Vector3 stemPosNDC = Vector3.Transform(stemPosWorld, totalMatrix);
                Vector2 stemPosPixel = new Vector2((1 + stemPosNDC.X) * 511, (1 - stemPosNDC.Y) * 511);

                Vector2 stemVector = new Vector2(selfPosPixel.X - basePosPixel.X, selfPosPixel.Y - basePosPixel.Y);
                float stemLength = stemVector.Length();
                float stemAngle = (float)Math.Atan2(stemVector.Y, stemVector.X);

                MySprite stemSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = stemPosPixel,
                    Size = new Vector2(stemLength, 1),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = stemAngle,
                };

                DepthSprite stemDepthSprite = new DepthSprite(stemSprite, stemPosNDC.Z);

                Vector3 cameraTargetNDC = Vector3.Transform(cameraTargetWorld.Translation, totalMatrix);

                MySprite gridSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grid_0",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = new Color(128, 128, 128, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                DepthSprite gridDepthSprite = new DepthSprite(gridSprite, cameraTargetNDC.Z);

                MySprite gradSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grad_0",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = new Color(255, 36, 75, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                DepthSprite gradDepthSprite = new DepthSprite(gradSprite, cameraTargetNDC.Z);

                _planeSprites.Clear();
                _planeSprites.Add(gridDepthSprite);
                _planeSprites.Add(gradDepthSprite);
                _staticSpritesAfterPlane.Clear();
                _staticSpritesAfterPlane.Add(selfDepthSprite);
                _staticSpritesAfterPlane.Add(baseDepthSprite);
                _staticSpritesAfterPlane.Add(stemDepthSprite);
            }

            public void BuildSprites()
            {
                EntitySprites.Clear();
                FinalSprites.Clear();
                _spritesBeforePlane.Clear();
                _spritesAfterPlane.Clear();

                Matrix cameraTargetWorld = _referenceBlock.WorldMatrix;
                Vector3 cameraPositionLocal = new Vector3(31334, 30557, 63764);
                Vector3 cameraPositionWorld = Vector3.Transform(cameraPositionLocal, cameraTargetWorld);

                Vector3 TargetToCamera = cameraPositionWorld - cameraTargetWorld.Translation;
                float TargetToCameraDist = TargetToCamera.Length();
                Vector3 TargetToCameraDir = TargetToCamera / TargetToCameraDist;

                cameraPositionWorld = cameraTargetWorld.Translation + TargetToCameraDir * (TargetToCameraDist / _zoom);

                Matrix viewMatrix = Matrix.CreateLookAt(cameraPositionWorld, cameraTargetWorld.Translation, cameraTargetWorld.Up);
                Matrix totalMatrix = viewMatrix * _projectionMatrix;

                Plane gridPlaneWorld = new Plane(cameraTargetWorld.Translation, cameraTargetWorld.Up);

                foreach (var entity in _entities.Values)
                {
                    Vector3 entityPosWorld = entity.Position;
                    Vector3 entityPosNDC = Vector3.Transform(entityPosWorld, totalMatrix);
                    Vector2 entityPosPixel = new Vector2((1 + entityPosNDC.X) * 511, (1 - entityPosNDC.Y) * 511);
                    float entityPosZView = -(_f * _n) / (_f - entityPosNDC.Z * (_f - _n));

                    float entityDepthScale = _minScale + (_maxScale - _minScale) * (-entityPosZView - _n) / (_f - _n);

                    string spriteName = default(string);
                    Vector2 spriteSize = default(Vector2);
                    Color spriteColor = default(Color);

                    if (entity is MissileInfo)
                    {
                        spriteName = "Missile_0";
                        spriteSize = new Vector2(16, 16);

                        var missile = entity as MissileInfo;

                        if (missile.LauncherID == _selfID)
                        {
                            spriteColor = Color.Cyan;
                        }
                        else if (_neutralIDs.Contains(missile.LauncherID))
                        {
                            spriteColor = Color.Orange;
                        }
                        else if (_friendlyIDs.Contains(missile.LauncherID))
                        {
                            spriteColor = Color.Lime;
                        }
                        else if (_hostileIDs.Contains(missile.LauncherID))
                        {
                            spriteColor = Color.OrangeRed;
                        }
                        else
                        {
                            spriteColor = Color.White;
                        }
                    }
                    else
                    {
                        if (_localIDs.Contains(entity.EntityID) && _remoteIDs.Contains(entity.EntityID))
                        {
                            spriteName = "Target_2";
                            spriteSize = new Vector2(32, 32);
                        }
                        else if (_localIDs.Contains(entity.EntityID))
                        {
                            spriteName = "Target_0";
                            spriteSize = new Vector2(32, 32);
                        }
                        else if (_remoteIDs.Contains(entity.EntityID))
                        {
                            spriteName = "Target_1";
                            spriteSize = new Vector2(32, 32);
                        }
                        else
                        {
                            spriteName = "Target_0";
                            spriteSize = new Vector2(32, 32);
                        }

                        if (_neutralIDs.Contains(entity.EntityID))
                        {
                            spriteColor = Color.Orange;
                        }
                        else if (_friendlyIDs.Contains(entity.EntityID))
                        {
                            spriteColor = Color.Lime;
                        }
                        else if (_hostileIDs.Contains(entity.EntityID))
                        {
                            spriteColor = Color.OrangeRed;
                        }
                        else
                        {
                            spriteColor = Color.White;
                        }
                    }

                    MySprite entitySprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = spriteName,
                        Position = entityPosPixel,
                        Size = spriteSize * entityDepthScale,
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    DepthSprite entityDepthSprite = new DepthSprite(entitySprite, entityPosNDC.Z);

                    EntitySprites.Add(entity.EntityID, entityDepthSprite);

                    Vector3 basePosWorld = entityPosWorld - (Vector3.Dot(gridPlaneWorld.Normal, entityPosWorld) + gridPlaneWorld.D) * gridPlaneWorld.Normal;
                    Vector3 basePosNDC = Vector3.Transform(basePosWorld, totalMatrix);
                    Vector2 basePosPixel = new Vector2((1 + basePosNDC.X) * 511, (1 - basePosNDC.Y) * 511);
                    float basePosZView = -(_f * _n) / (_f - basePosNDC.Z * (_f - _n));

                    float baseDepthScale = _minScale + (_maxScale - _minScale) * (-basePosZView - _n) / (_f + _n);

                    MySprite baseSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Base_0",
                        Position = basePosPixel,
                        Size = new Vector2(32, 32) * baseDepthScale,
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    DepthSprite baseDepthSprite = new DepthSprite(baseSprite, basePosNDC.Z);

                    Vector3 stemPosWorld = 0.5f * (entityPosWorld + basePosWorld);
                    Vector3 stemPosNDC = Vector3.Transform(stemPosWorld, totalMatrix);
                    Vector2 stemPosPixel = new Vector2((1 + stemPosNDC.X) * 511, (1 - stemPosNDC.Y) * 511);

                    Vector2 stemVector = new Vector2(entityPosPixel.X - basePosPixel.X, entityPosPixel.Y - basePosPixel.Y);
                    float stemLength = stemVector.Length();
                    float stemAngle = (float)Math.Atan2(stemVector.Y, stemVector.X);

                    MySprite stemSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = stemPosPixel,
                        Size = new Vector2(stemLength, 1),
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = stemAngle,
                    };

                    DepthSprite stemDepthSprite = new DepthSprite(stemSprite, stemPosNDC.Z);

                    if ((Vector3.Dot(cameraPositionWorld, gridPlaneWorld.Normal) + gridPlaneWorld.D) * (Vector3.Dot(entityPosWorld, gridPlaneWorld.Normal) + gridPlaneWorld.D) > 0)
                    {
                        _spritesAfterPlane.Add(entityDepthSprite);
                        _spritesAfterPlane.Add(baseDepthSprite);
                        _spritesAfterPlane.Add(stemDepthSprite);
                    }
                    else
                    {
                        _spritesBeforePlane.Add(entityDepthSprite);
                        _spritesBeforePlane.Add(baseDepthSprite);
                        _spritesBeforePlane.Add(stemDepthSprite);
                    }
                }

                FinalSprites.AddRange(_spritesBeforePlane.OrderBy(x => -x.Depth));
                FinalSprites.AddRange(_planeSprites);
                FinalSprites.AddRange(_spritesAfterPlane.Concat(_staticSpritesAfterPlane).OrderBy(x => -x.Depth));
            }
        }
    }
}
