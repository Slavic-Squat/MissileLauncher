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
            #region Properties
            public Program Program { get; private set; }
            public int ID { get; private set; }
            public Dictionary<long, TargetInfo> Targets { get; set; }
            public Dictionary<long, MissileInfo> Missiles { get; set; }
            public long SelectedTarget { get; set; }
            public long SelectedMissile { get; set; }
            #endregion

            #region Parts
            private IMyTextSurface _display;
            private IMyTerminalBlock _reference;
            #endregion

            #region State Info
            private int _runCounter;
            #endregion

            public TargetingUI(Program program, int id, IMyTextSurface display, IMyTerminalBlock reference)
            {
                Program = program;
                ID = id;
                _display = display;
                _reference = reference;

                SetupDrawSurface(_display);
            }

            public void Run(DateTime time)
            {
                Program.Echo("UI Running");
                _runCounter++;
                _runCounter %= 10;

                if (_runCounter == 9)
                {
                    var frame = _display.DrawFrame();
                    DrawBackground(frame, new Vector2(256, 256));
                    DrawRangeVectors(frame, new Vector2(256, 256));
                    DrawTargets(frame, new Vector2(256, 256));
                    DrawMissiles(frame, new Vector2(256, 256));
                    DrawTargetSelector(frame, new Vector2(256, 256));
                    frame.Dispose();
                }
            }

            public void AddTargets(Dictionary<long, MyTuple<Vector3, Vector3, long>> Targets)
            {
                this.Targets.Clear();

                foreach (var target in Targets)
                {
                    var targetLocalPos = Vector3.TransformNormal(target.Value.Item1 - _reference.GetPosition(), Matrix.Transpose(_reference.WorldMatrix));
                    var targetLocalVel = Vector3.TransformNormal(target.Value.Item2, Matrix.Transpose(_reference.WorldMatrix));
                    var timeDetected = new DateTime(target.Value.Item3);

                    this.Targets[target.Key] = new MyTuple<Vector3, Vector3, DateTime>(targetLocalPos, targetLocalVel, timeDetected);
                }
            }

            public void AddMissiles(Dictionary<string, MyTuple<MyTuple<string, long, long>, MyTuple<Vector3, Vector3, Vector3>>> Missiles)
            {
                this.Missiles.Clear();

                foreach (var missile in Missiles)
                {
                    var missileLocalPos = Vector3.TransformNormal(missile.Value.Item2.Item1 - _reference.GetPosition(), Matrix.Transpose(_reference.WorldMatrix));
                    var missileLocalVel = Vector3.TransformNormal(missile.Value.Item2.Item2, Matrix.Transpose(_reference.WorldMatrix));
                    var missileLocalHeadingVector = Vector3.TransformNormal(missile.Value.Item2.Item3, Matrix.Transpose(_reference.WorldMatrix));
                    var timeRecieved = new DateTime(missile.Value.Item1.Item3);

                    this.Missiles[missile.Key] = new MyTuple<MyTuple<string, long, DateTime>, MyTuple<Vector3, Vector3, Vector3>>()
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
                foreach (var target in Targets)
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
                foreach (var missile in Missiles)
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
                foreach (var missile in Missiles)
                {
                    if (Targets.ContainsKey(missile.Value.Item1.Item2) && missile.Value.Item1.Item1 != "Idle" && missile.Value.Item1.Item1 != "Launching")
                    {
                        var rangeVector = Targets[missile.Value.Item1.Item2].Item1 - missile.Value.Item2.Item1;
                        var angle = (float)Math.Atan2(-rangeVector.X, -rangeVector.Z);
                        var position = (Targets[missile.Value.Item1.Item2].Item1 + missile.Value.Item2.Item1) / 2;
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
                if (Targets.ContainsKey(SelectedTarget))
                {
                    var position = Targets[SelectedTarget].Item1;

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

            }
        }
    }
}
