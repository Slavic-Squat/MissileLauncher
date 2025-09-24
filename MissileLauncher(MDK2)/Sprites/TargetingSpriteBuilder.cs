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
            #endregion

            #region Parts
            private IMyCubeGrid _referenceGrid;
            #endregion

            #region State Info
            private int _runCounter;
            #endregion

            #region Fields
            private float _FOV = 30;
            private float _AR = 1;
            private float _n = 100;
            private float _f = 100000;
            private float _minScale = 0.5f;
            private float _maxScale = 1.5f;

            private Dictionary<long, EntityInfo> _entities;
            private HashSet<long> _neutralIDs;
            private HashSet<long> _friendlyIDs;
            private HashSet<long> _hostileIDs;
            private HashSet<long> _localIDs;
            private HashSet<long> _remoteIDs;
            private long _selfID = -1;

            private MySprite _radialGridSprite;
            private MySprite _radialGradientSprite;

            private List<DepthSprite> _spritesBeforePlane = new List<DepthSprite>();
            private List<DepthSprite> _spritesAfterPlane = new List<DepthSprite>();

            private Matrix _worldMatrix = Matrix.Identity;
            private Matrix _viewMatrix = Matrix.Identity;
            private Matrix _projectionMatrix = Matrix.Identity;
            private Plane _gridPlaneView = new Plane();
            private Vector3 _cameraTargetView = Vector3.Zero;
            #endregion

            public TargetingSpriteBuilder(IMyCubeGrid referenceGrid, Dictionary<long, EntityInfo> entities, HashSet<long> neutralIDs, HashSet<long> friendlyIDs, HashSet<long> hostileIDs, HashSet<long> localIDs, HashSet<long> remoteIDs, long selfID)
            {
                _referenceGrid = referenceGrid;
                _entities = entities;
                _selfID = selfID;

                _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n, _f);
                EntitySprites = new Dictionary<long, DepthSprite>();
                FinalSprites = new List<DepthSprite>();

                _radialGridSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grid_0",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                _radialGradientSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grad_0",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };
            }

            public void Run()
            {
                _runCounter++;
                _runCounter %= 10;

                if (_runCounter == 9)
                {
                    BuildSprites();
                }
            }

            public void BuildSprites()
            {
                EntitySprites.Clear();
                FinalSprites.Clear();
                _spritesBeforePlane.Clear();
                _spritesAfterPlane.Clear();

                _worldMatrix = _referenceGrid.WorldMatrix;

                Vector3 cameraPositionLocal = new Vector3(17861, 14241, 30238);
                Vector3 cameraPositionWorld = Vector3.Transform(cameraPositionLocal, _worldMatrix);

                _viewMatrix = Matrix.CreateLookAt(cameraPositionWorld, _worldMatrix.Translation, _worldMatrix.Up);

                _gridPlaneView = Plane.Transform(new Plane(_worldMatrix.Translation, _worldMatrix.Up), _viewMatrix);
                _cameraTargetView = Vector3.Transform(_worldMatrix.Translation, _viewMatrix);

                DepthSprite radialGridDepthSprite = new DepthSprite(_radialGridSprite, _cameraTargetView);
                DepthSprite radialGradientDepthSprite = new DepthSprite(_radialGradientSprite, _cameraTargetView);
                DepthSprite selfDepthSprite = new DepthSprite(_selfSprite, _cameraTargetView);

                foreach (var entity in _entities.Values)
                {
                    Vector3 entityPosView = Vector3.Transform(entity.Position, _viewMatrix);
                    Vector3 entityPosNDC = Vector3.Transform(entityPosView, _projectionMatrix);

                    float depthScale = _minScale + (_maxScale - _minScale) * (entityPosView.Z - _n) / (_f - _n);

                    string spriteName = default(string);
                    Vector2 spriteSize = default(Vector2);
                    Color spriteColor = default(Color);

                    if (entity is MissileInfo)
                    {
                        spriteName = "Missile";
                        spriteSize = new Vector2(64, 64);

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
                    }
                    else
                    {
                        if (_localIDs.Contains(entity.EntityID) && _remoteIDs.Contains(entity.EntityID))
                        {
                            spriteName = "Target2";
                            spriteSize = new Vector2(128, 128);
                        }
                        else if (_localIDs.Contains(entity.EntityID))
                        {
                            spriteName = "Target0";
                            spriteSize = new Vector2(128, 128);
                        }
                        else if (_remoteIDs.Contains(entity.EntityID))
                        {
                            spriteName = "Target1";
                            spriteSize = new Vector2(128, 128);
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
                    }

                    MySprite sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = spriteName,
                        Position = new Vector2((1 + entityPosNDC.X) * 511, (1 - entityPosNDC.Y) * 511),
                        Size = spriteSize * depthScale,
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    DepthSprite entityDepthSprite = new DepthSprite(sprite, entityPosView);

                    EntitySprites.Add(entity.EntityID, entityDepthSprite);

                    Vector3 basePosView = entityPosView - (Vector3.Dot(_gridPlaneView.Normal, entityPosView) + _gridPlaneView.D) * _gridPlaneView.Normal;
                    Vector3 basePosNDC = Vector3.Transform(basePosView, _projectionMatrix);

                    depthScale = _minScale + (_maxScale - _minScale) * (basePosView.Z - _n) / (_f + _n);

                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Sprite_Base",
                        Position = new Vector2((1 + basePosNDC.X) * 511, (1 - basePosNDC.Y) * 511),
                        Size = new Vector2(32, 32) * depthScale,
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    DepthSprite baseDepthSprite = new DepthSprite(sprite, basePosView);

                    Vector3 stemPosView = 0.5f * (entityPosView + basePosView);
                    Vector3 stemPosNDC = Vector3.Transform(stemPosView, _projectionMatrix);

                    Vector2 stemVector = new Vector2(entityPosNDC.X - basePosNDC.X, entityPosNDC.Y - basePosNDC.Y) * new Vector2(1024, 615);
                    float stemLength = stemVector.Length();
                    float stemAngle = (float)Math.Atan2(stemVector.Y, stemVector.X);

                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = new Vector2((1 + stemPosNDC.X) * 511, (1 - stemPosNDC.Y) * 511),
                        Size = new Vector2(stemLength, 1),
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = stemAngle,
                    };

                    DepthSprite stemDepthSprite = new DepthSprite(sprite, stemPosView);

                    if (_gridPlaneView.D * (Vector3.Dot(entityPosView, _gridPlaneView.Normal) + _gridPlaneView.D) > 0)
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

                FinalSprites.AddRange(_spritesBeforePlane.OrderBy(x => x.Depth));
                FinalSprites.Add(selfDepthSprite);
                FinalSprites.Add(radialGridDepthSprite);
                FinalSprites.Add(radialGradientDepthSprite);
                FinalSprites.AddRange(_spritesAfterPlane.OrderBy(x => x.Depth));
            }
        }
    }
}
