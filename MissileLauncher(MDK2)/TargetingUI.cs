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
        public class TargetingUI
        {
            #region General Info
            private Program program;
            private int ID;
            #endregion

            #region Parts
            private IMyTextSurface display;
            private IMyTerminalBlock reference;
            #endregion

            #region State Info
            private Dictionary<long, MyTuple<Vector3, Vector3, DateTime>> targets = new Dictionary<long, MyTuple<Vector3, Vector3, DateTime>>();
            private Dictionary<string, MyTuple<MyTuple<string, long, DateTime>, MyTuple<Vector3, Vector3, Vector3>>> missiles = new Dictionary<string, MyTuple<MyTuple<string, long, DateTime>, MyTuple<Vector3, Vector3, Vector3>>>();
            public long selectedTarget;
            private int runCounter;
            #endregion

            public TargetingUI(Program program, int ID, IMyTextSurface display, IMyTerminalBlock reference)
            {
                this.program = program;
                this.ID = ID;
                this.display = display;
                this.reference = reference;

                SetupDrawSurface(display);
            }

            public void Run(DateTime time)
            {
                program.Echo("UI Running");
                runCounter++;
                runCounter %= 10;

                if (runCounter == 9)
                {
                    var frame = display.DrawFrame();
                    DrawBackground(frame, new Vector2(256, 256));
                    DrawRangeVectors(frame, new Vector2(256, 256));
                    DrawTargets(frame, new Vector2(256, 256));
                    DrawMissiles(frame, new Vector2(256, 256));
                    DrawTargetSelector(frame, new Vector2(256, 256));
                    frame.Dispose();
                }
            }

            public void AddTargets(Dictionary<long, MyTuple<Vector3, Vector3, long>> targets)
            {
                this.targets.Clear();

                foreach (var target in targets)
                {
                    var targetLocalPos = Vector3.TransformNormal(target.Value.Item1 - reference.GetPosition(), Matrix.Transpose(reference.WorldMatrix));
                    var targetLocalVel = Vector3.TransformNormal(target.Value.Item2, Matrix.Transpose(reference.WorldMatrix));
                    var timeDetected = new DateTime(target.Value.Item3);

                    this.targets[target.Key] = new MyTuple<Vector3, Vector3, DateTime>(targetLocalPos, targetLocalVel, timeDetected);
                }
            }

            public void AddMissiles(Dictionary<string, MyTuple<MyTuple<string, long, long>, MyTuple<Vector3, Vector3, Vector3>>> missiles)
            {
                this.missiles.Clear();

                foreach (var missile in missiles)
                {
                    var missileLocalPos = Vector3.TransformNormal(missile.Value.Item2.Item1 - reference.GetPosition(), Matrix.Transpose(reference.WorldMatrix));
                    var missileLocalVel = Vector3.TransformNormal(missile.Value.Item2.Item2, Matrix.Transpose(reference.WorldMatrix));
                    var missileLocalHeadingVector = Vector3.TransformNormal(missile.Value.Item2.Item3, Matrix.Transpose(reference.WorldMatrix));
                    var timeRecieved = new DateTime(missile.Value.Item1.Item3);

                    this.missiles[missile.Key] = new MyTuple<MyTuple<string, long, DateTime>, MyTuple<Vector3, Vector3, Vector3>>()
                    {
                        Item1 = new MyTuple<string, long, DateTime>(missile.Value.Item1.Item1, missile.Value.Item1.Item2, timeRecieved),
                        Item2 = new MyTuple<Vector3, Vector3, Vector3>(missileLocalPos, missileLocalVel, missileLocalHeadingVector)
                    };
                }
            }

            public void SetupDrawSurface(IMyTextSurface surface)
            {
                // Draw background color
                surface.ScriptBackgroundColor = new Color(0, 10, 0, 255);

                // Set content type
                surface.ContentType = ContentType.SCRIPT;

                // Set script to none
                surface.Script = "";
            }

            public void DrawTargets(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                foreach (var target in targets)
                {
                    var position = target.Value.Item1;
                    frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "CircleHollow",
                        Position = new Vector2(position.X / 40, position.Z / 40) * scale + centerPos,
                        Size = new Vector2(20f, 20f) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    });
                }
            }

            public void DrawMissiles(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                foreach (var missile in missiles)
                {
                    if (missile.Value.Item1.Item1 != "Idle" && missile.Value.Item1.Item1 != "Launching")
                    {
                        var position = missile.Value.Item2.Item1;
                        var angle = (float)Math.Atan2(-missile.Value.Item2.Item3.X, -missile.Value.Item2.Item3.Z);
                        frame.Add(new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Alignment = TextAlignment.CENTER,
                            Data = "Triangle",
                            Position = new Vector2(position.X / 40, position.Z / 40) * scale + centerPos,
                            Size = new Vector2(8f, 16f) * scale,
                            Color = new Color(0, 255, 0, 255),
                            RotationOrScale = -angle
                        });
                    }
                }
            }

            public void DrawRangeVectors(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                foreach (var missile in missiles)
                {
                    if (targets.ContainsKey(missile.Value.Item1.Item2) && missile.Value.Item1.Item1 != "Idle" && missile.Value.Item1.Item1 != "Launching")
                    {
                        var rangeVector = targets[missile.Value.Item1.Item2].Item1 - missile.Value.Item2.Item1;
                        var angle = (float)Math.Atan2(-rangeVector.X, -rangeVector.Z);
                        var position = (targets[missile.Value.Item1.Item2].Item1 + missile.Value.Item2.Item1) / 2;
                        var length = (float)Math.Sqrt(rangeVector.X * rangeVector.X + rangeVector.Z * rangeVector.Z);

                        frame.Add(new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Alignment = TextAlignment.CENTER,
                            Data = "SquareSimple",
                            Position = new Vector2(position.X / 40, position.Z / 40) * scale + centerPos,
                            Size = new Vector2(1f, length / 40) * scale,
                            Color = new Color(0, 60, 0, 255),
                            RotationOrScale = -angle
                        });
                    }
                }
            }

            public void DrawTargetSelector(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                if (targets.ContainsKey(selectedTarget))
                {
                    var position = targets[selectedTarget].Item1;

                    frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "Circle",
                        Position = new Vector2(position.X / 40, position.Z / 40) * scale + centerPos,
                        Size = new Vector2(14f, 14f) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    });
                }
            }

            public void DrawBackground(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(500f, 500f) * scale,
                    Color = new Color(0, 20, 0, 255),
                    RotationOrScale = 0f
                }); // Range10km
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "Circle",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(492f, 492f) * scale,
                    Color = new Color(0, 10, 0, 255),
                    RotationOrScale = 0f
                }); // Range10kmClip
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(400f, 400f) * scale,
                    Color = new Color(0, 20, 0, 255),
                    RotationOrScale = 0f
                }); // Range8km
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "Circle",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(392f, 392f) * scale,
                    Color = new Color(0, 10, 0, 255),
                    RotationOrScale = 0f
                }); // Range8kmClip
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(300f, 300f) * scale,
                    Color = new Color(0, 20, 0, 255),
                    RotationOrScale = 0f
                }); // Range6km
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "Circle",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(293f, 293f) * scale,
                    Color = new Color(0, 10, 0, 255),
                    RotationOrScale = 0f
                }); // Range6kmClip
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(200f, 200f) * scale,
                    Color = new Color(0, 20, 0, 255),
                    RotationOrScale = 0f
                }); // Range4km
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "Circle",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(194f, 194f) * scale,
                    Color = new Color(0, 10, 0, 255),
                    RotationOrScale = 0f
                }); // Range4kmClip
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(100f, 100f) * scale,
                    Color = new Color(0, 20, 0, 255),
                    RotationOrScale = 0f
                }); // Range2km
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "Grid",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(512f, 512f) * scale,
                    Color = new Color(0, 50, 0, 255),
                    RotationOrScale = 0f
                }); // Grid
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = "10km",
                    Position = new Vector2(0f, -245f) * scale + centerPos,
                    Color = new Color(0, 50, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = 0.35f * scale
                }); // 10kmLabel
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = "8km",
                    Position = new Vector2(0f, -195f) * scale + centerPos,
                    Color = new Color(0, 50, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = 0.35f * scale
                }); // 8kmLabel
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = "6km",
                    Position = new Vector2(0f, -145f) * scale + centerPos,
                    Color = new Color(0, 50, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = 0.35f * scale
                }); // 6kmLabel
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = "4km",
                    Position = new Vector2(0f, -95f) * scale + centerPos,
                    Color = new Color(0, 50, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = 0.35f * scale
                }); // 4kmLabel
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = "2km",
                    Position = new Vector2(0f, -45f) * scale + centerPos,
                    Color = new Color(0, 50, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = 0.35f * scale
                }); // 2kmLabel
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "AH_BoreSight",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(50f, 50f) * scale,
                    Color = new Color(0, 255, 0, 255),
                    RotationOrScale = -1.5708f
                }); // Launcher

                /*
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(-128f, -128f) * scale + centerPos,
                    Size = new Vector2(20f, 20f) * scale,
                    Color = new Color(0, 255, 0, 255),
                    RotationOrScale = 0f
                }); // DummyTarget
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "Circle",
                    Position = new Vector2(-128f, -128f) * scale + centerPos,
                    Size = new Vector2(14f, 14f) * scale,
                    Color = new Color(0, 255, 0, 255),
                    RotationOrScale = 0f
                }); // TargetSelector
                */
            }
        }
    }
}
