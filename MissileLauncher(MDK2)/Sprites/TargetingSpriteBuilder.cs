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
            public TargetCoordinator TargetCoordinator;
            public Dictionary<long, EntityInfo> Targets { get; set; }
            public List<EntitySprite3D> TargetSprites { get; private set; }
            public Dictionary<long, EntityInfo> Missiles { get; set; }
            public List<EntitySprite3D> MissileSprites { get; private set; }
            public List<ISprite3D> FinalSprites { get; private set; }
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

            private Sprite3D _radialGridSprite;
            private Sprite3D _radialGradientSprite;

            private List<Sprite3D> _spritesBeforePlane = new List<Sprite3D>();
            private List<Sprite3D> _spritesAfterPlane = new List<Sprite3D>();
            private Matrix _worldMatrix = Matrix.Identity;
            private Matrix _viewMatrix = Matrix.Identity;
            private Matrix _projectionMatrix = Matrix.Identity;
            private Plane _gridPlaneView = new Plane();
            #endregion

            public TargetingSpriteBuilder(TargetCoordinator targetCoordinator, IMyCubeGrid referenceGrid, float FOV, float AR, float n, float f)
            {
                TargetCoordinator = targetCoordinator;
                Missiles = TargetCoordinator.Missiles;
                Targets = TargetCoordinator.Targets;

                _referenceGrid = referenceGrid;
                _FOV = FOV;
                _AR = AR;
                _n = n;
                _f = f;

                _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n, _f);
                TargetSprites = new List<Sprite3D>();
                MissileSprites = new List<Sprite3D>();
                FinalSprites = new List<Sprite3D>();
            }

            public void Run()
            {
                _runCounter++;
                _runCounter %= 10;

                if (_runCounter == 9)
                {
                    BuildSprite3Ds();
                }
            }

            public void BuildSprite3Ds()
            {
                _worldMatrix = _referenceGrid.WorldMatrix;

                Vector3 cameraPositionLocal = new Vector3(17861, 14241, 30238);
                Vector3 cameraPositionWorld = Vector3.Transform(cameraPositionLocal, _worldMatrix);

                _viewMatrix = Matrix.CreateLookAt(cameraPositionWorld, _worldMatrix.Translation, _worldMatrix.Up);

                _gridPlaneView = Plane.Transform(new Plane(_worldMatrix.Translation, _worldMatrix.Up), _viewMatrix);

                Vector3 radialSpritePosView = Vector3.Transform(_worldMatrix.Translation, _viewMatrix);
                Vector3 radialSpritePosNDC = Vector3.Transform(radialSpritePosView, _projectionMatrix);
                _radialGridSprite = Sprite3D.CreateSprite3D(Sprite3D.Sprite3DType.Misc, _radialGridSpriteName, -1, radialSpritePosNDC, _radialSpriteNativeSize, 1, 0, _radialGridSpriteColor);
                _radialGradientSprite = Sprite3D.CreateSprite3D(Sprite3D.Sprite3DType.Misc, _radialGradientSpriteName, -1, radialSpritePosNDC, _radialSpriteNativeSize, 1, 0, _radialGradientSpriteColor);

                TargetSprites.Clear();
                MissileSprites.Clear();
                _spritesBeforePlane.Clear();
                _spritesAfterPlane.Clear();

                foreach(var target in Targets)
                {
                    Vector3 targetPosView = Vector3.Transform(target.Value.Position, _viewMatrix);
                    Vector3 targetPosNDC = Vector3.Transform(targetPosView, _projectionMatrix);

                    float depthScale = _targetSpriteMinScale + (_targetSpriteMaxScale - _targetSpriteMinScale) * (targetPosView.Z - _n) / (_f - _n);
                    Sprite3D targetSprite = Sprite3D.CreateSprite3D(Sprite3D.Sprite3DType.Target, _targetSpriteName, target.Value.EntityID, targetPosNDC, _targetSpriteNativeSize, depthScale, 0, _targetSpriteColor);
                    TargetSprites.Add(targetSprite);

                    Vector3 basePosView = targetPosView - (Vector3.Dot(_gridPlaneView.Normal, targetPosView) + _gridPlaneView.D) * _gridPlaneView.Normal;
                    Vector3 basePosNDC = Vector3.Transform(basePosView, _projectionMatrix);
                    depthScale = _baseSpriteMinScale + (_baseSpriteMaxScale - _baseSpriteMinScale) * (basePosView.Z - _n) / (_f + _n);
                    Sprite3D baseSprite = Sprite3D.CreateSprite3D(Sprite3D.Sprite3DType.Base, _baseSpriteName, target.Value.EntityID, basePosNDC, _baseSpriteNativeSize, depthScale, 0, _baseSpriteColor);

                    Sprite3D stemSprite = Sprite3D.CreateVectorSprite3D(basePosNDC, targetPosNDC, _stemSpriteColor);

                    if (Vector3.Dot(targetPosView, _gridPlaneView.Normal) + _gridPlaneView.D > 0)
                    {
                        _spritesAfterPlane.Add(targetSprite);
                        _spritesAfterPlane.Add(baseSprite);
                        _spritesAfterPlane.Add(stemSprite);
                    }
                    else
                    {
                        _spritesBeforePlane.Add(targetSprite);
                        _spritesBeforePlane.Add(baseSprite);
                        _spritesBeforePlane.Add(stemSprite);
                    }
                }

                foreach(var missile in Missiles)
                {

                }

                _spritesBeforePlane = _spritesBeforePlane.OrderBy(x => x.Z).ThenBy(x => x.Type3D).ToList();
                _spritesBeforePlane.Add(_radialGradientSprite);
                _spritesBeforePlane.Add(_radialGridSprite);
                _spritesAfterPlane = _spritesAfterPlane.OrderBy(x => x.Z).ThenBy(x => x.Type3D).ToList();
                FinalSprites = _spritesBeforePlane.Concat(_spritesAfterPlane).ToList();
            }
        }
    }
}
