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
        public class TargetingSpriteBuilderSimple
        {
            #region Properties
            public float Range
            {
                get { return _range; }
                set { _range = value; }
            }
            #endregion

            #region Fields
            private float _range = 12000f;

            private List<MySpriteExt> _sprites = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSprites = new List<MySpriteExt>();
            #endregion

            public TargetingSpriteBuilderSimple()
            {
                BuildStaticSprites();
            }

            private void BuildStaticSprites()
            {
                MySprite tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Self_1",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(128, 128),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt selfSpriteExt = new MySpriteExt(tempSprite, 0.03f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grid_1",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = new Color(128, 128, 128, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gridSpriteExt = new MySpriteExt(tempSprite, 0.02f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grad_1",
                    Position = new Vector2(511, 511),
                    Size = new Vector2(1024, 1024),
                    Color = new Color(1, 89, 68, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gradSpriteExt = new MySpriteExt(tempSprite, 0.01f);

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

                MySpriteExt bgSpriteExt = new MySpriteExt(tempSprite, -100000f);

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

                MySpriteExt bgFillSpriteExt = new MySpriteExt(tempSprite, -100001f);

                _staticSprites.Clear();
                _staticSprites.Add(bgSpriteExt);
                _staticSprites.Add(bgFillSpriteExt);
                _staticSprites.Add(gridSpriteExt);
                _staticSprites.Add(gradSpriteExt);
                _staticSprites.Add(selfSpriteExt);
            }

            public List<MySpriteExt> BuildSprites(Dictionary<long, EntityInfoExt> entityInfoExts, out Dictionary<long, MyEntitySprite> entitySprites, long targetedID = -1)
            {
                List<MySpriteExt> finalSprites = new List<MySpriteExt>();
                entitySprites = new Dictionary<long, MyEntitySprite>();

                _sprites.Clear();

                Matrix referenceWorldMatrix = SystemCoordinator.ReferenceWorldMatrix;
                float pixelsPerMeter = 512f / _range;

                foreach (var entityInfoExtKVP in entityInfoExts)
                {
                    EntityInfoExt entityInfoExt = entityInfoExtKVP.Value;
                    long key = entityInfoExtKVP.Key;

                    float distance = Vector3.Distance(referenceWorldMatrix.Translation, entityInfoExt.Position);

                    if (distance > _range)
                    {
                        continue;
                    }

                    EntityInfo entityInfo = entityInfoExt.Info;

                    Vector3 entityPosWorld = entityInfo.Position;
                    Vector3 entityPosLocal = Vector3.TransformNormal(entityPosWorld - referenceWorldMatrix.Translation, Matrix.Transpose(referenceWorldMatrix.GetOrientation()));
                    Vector2 entityPosPixel = new Vector2(511 + entityPosLocal.X * pixelsPerMeter, 511 + entityPosLocal.Z * pixelsPerMeter);

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
                        Size = spriteSize,
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    MySpriteExt MySpriteExtEntity = new MySpriteExt(tempSprite, entityPosLocal.Y);
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

                        selectorSpriteExt = new MySpriteExt(tempSprite, entityPosLocal.Y + 0.001f);
                    }

                    _sprites.Add(MySpriteExtEntity);

                    if (selectorSpriteExt.IsValid)
                    {
                        _sprites.Add(selectorSpriteExt);
                    }
                }

                finalSprites.AddRange(_sprites.Concat(_staticSprites).OrderBy(x => x.Depth));

                return finalSprites;
            }
        }
    }
}
