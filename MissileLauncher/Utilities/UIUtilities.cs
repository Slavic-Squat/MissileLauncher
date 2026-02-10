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
        public static class UIUtilities
        {
            public static T Navigate<T>(IEnumerable<T> source, T current, Direction direction) where T : IPositional2D
            {
                float epsilon = 0.001f;
                if (EqualityComparer<T>.Default.Equals(current, default(T)))
                {
                    current = source.OrderBy(element => element.Pos.X + 2 * element.Pos.Y).FirstOrDefault();
                }

                T next = default(T);
                float min = float.MaxValue;
                switch (direction)
                {
                    case Direction.Left:
                        foreach (var element in source)
                        {
                            if (element.Pos.X < current.Pos.X - epsilon)
                            {
                                float dx = Math.Abs(element.Pos.X - current.Pos.X);
                                float dy = Math.Abs(element.Pos.Y - current.Pos.Y);
                                float distance = dx + 10 * dy;
                                if (distance < min)
                                {
                                    min = distance;
                                    next = element;
                                }
                            }
                        }
                        break;
                    case Direction.Right:
                        foreach (var element in source)
                        {
                            if (element.Pos.X > current.Pos.X + epsilon)
                            {
                                float dx = Math.Abs(element.Pos.X - current.Pos.X);
                                float dy = Math.Abs(element.Pos.Y - current.Pos.Y);
                                float distance = dx + 10 * dy;
                                if (distance < min)
                                {
                                    min = distance;
                                    next = element;
                                }
                            }
                        }
                        break;
                    case Direction.Up:
                        foreach (var element in source)
                        {
                            if (element.Pos.Y < current.Pos.Y - epsilon)
                            {
                                float dx = Math.Abs(element.Pos.X - current.Pos.X);
                                float dy = Math.Abs(element.Pos.Y - current.Pos.Y);
                                float distance = 10 * dx + dy;
                                if (distance < min)
                                {
                                    min = distance;
                                    next = element;
                                }
                            }
                        }
                        break;
                    case Direction.Down:
                        foreach (var element in source)
                        {
                            if (element.Pos.Y > current.Pos.Y + epsilon)
                            {
                                float dx = Math.Abs(element.Pos.X - current.Pos.X);
                                float dy = Math.Abs(element.Pos.Y - current.Pos.Y);
                                float distance = 10 * dx + dy;
                                if (distance < min)
                                {
                                    min = distance;
                                    next = element;
                                }
                            }
                        }
                        break;
                }

                if (EqualityComparer<T>.Default.Equals(next, default(T)))
                {
                    return current;
                }

                return next;
            }

            public static TKey Navigate<TKey, TValue>(Dictionary<TKey, TValue> source, TKey currentKey, Direction direction) where TValue : IPositional2D
            {
                float epsilon = 0.001f;
                if (!source.ContainsKey(currentKey))
                {
                    currentKey = source.Keys.OrderBy(k => source[k].Pos.X + 2 * source[k].Pos.Y).FirstOrDefault();
                }
                TKey nextKey = default(TKey);
                float min = float.MaxValue;
                switch (direction)
                {
                    case Direction.Left:
                        foreach (var kvp in source)
                        {
                            if (kvp.Value.Pos.X < source[currentKey].Pos.X - epsilon)
                            {
                                float dx = Math.Abs(kvp.Value.Pos.X - source[currentKey].Pos.X);
                                float dy = Math.Abs(kvp.Value.Pos.Y - source[currentKey].Pos.Y);
                                float distance = dx + 10 * dy;
                                if (distance < min)
                                {
                                    min = distance;
                                    nextKey = kvp.Key;
                                }
                            }
                        }
                        break;
                    case Direction.Right:
                        foreach (var kvp in source)
                        {
                            if (kvp.Value.Pos.X > source[currentKey].Pos.X + epsilon)
                            {
                                float dx = Math.Abs(kvp.Value.Pos.X - source[currentKey].Pos.X);
                                float dy = Math.Abs(kvp.Value.Pos.Y - source[currentKey].Pos.Y);
                                float distance = dx + 10 * dy;
                                if (distance < min)
                                {
                                    min = distance;
                                    nextKey = kvp.Key;
                                }
                            }
                        }
                        break;
                    case Direction.Up:
                        foreach (var kvp in source)
                        {
                            if (kvp.Value.Pos.Y < source[currentKey].Pos.Y - epsilon)
                            {
                                float dx = Math.Abs(kvp.Value.Pos.X - source[currentKey].Pos.X);
                                float dy = Math.Abs(kvp.Value.Pos.Y - source[currentKey].Pos.Y);
                                float distance = 10 * dx + dy;
                                if (distance < min)
                                {
                                    min = distance;
                                    nextKey = kvp.Key;
                                }
                            }
                        }
                        break;
                    case Direction.Down:
                        foreach (var kvp in source)
                        {
                            if (kvp.Value.Pos.Y > source[currentKey].Pos.Y + epsilon)
                            {
                                float dx = Math.Abs(kvp.Value.Pos.X - source[currentKey].Pos.X);
                                float dy = Math.Abs(kvp.Value.Pos.Y - source[currentKey].Pos.Y);
                                float distance = 10 * dx + dy;
                                if (distance < min)
                                {
                                    min = distance;
                                    nextKey = kvp.Key;
                                }
                            }
                        }
                        break;
                }

                if (EqualityComparer<TKey>.Default.Equals(nextKey, default(TKey)))
                {
                    return currentKey;
                }
                return nextKey;
            }

            public static float CalculateElementSize(float totalSize, int numElements, float spacingRatio)
            {
                if (numElements <= 0) return 0;
                return totalSize / (numElements + spacingRatio * (numElements - 1));
            }

            public static Color Lerp(Color colorA, Color colorB, float t)
            {
                t = MathHelper.Clamp(t, 0f, 1f);
                byte r = (byte)(colorA.R + (colorB.R - colorA.R) * t);
                byte g = (byte)(colorA.G + (colorB.G - colorA.G) * t);
                byte b = (byte)(colorA.B + (colorB.B - colorA.B) * t);
                byte a = (byte)(colorA.A + (colorB.A - colorA.A) * t);
                return new Color(r, g, b, a);
            }

            public static void AppendTime(StringBuilder sb, double totalSeconds)
            {
                if (double.IsPositiveInfinity(totalSeconds))
                {
                    sb.Append("∞");
                    return;
                }

                TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
                if (timeSpan.Days > 0)
                {
                    sb.Append(timeSpan.Days).Append("d, ").Append(timeSpan.Hours).Append("h");
                }
                else if (timeSpan.Hours > 0)
                {
                    sb.Append(timeSpan.Hours).Append("h, ").Append(timeSpan.Minutes).Append("m");
                }
                else if (timeSpan.Minutes > 0)
                {
                    sb.Append(timeSpan.Minutes).Append("m, ").Append(timeSpan.Seconds).Append("s");
                }
                else if (timeSpan.Seconds > 0)
                {
                    sb.Append(timeSpan.Seconds).Append("s");
                }
                else
                {
                    sb.Append(timeSpan.Milliseconds).Append("ms");
                }
            }

            public static void AppendVolume(StringBuilder sb, double volumeM3)
            {
                if (Math.Abs(volumeM3) >= 1)
                {
                    AppendNumber(sb, volumeM3);
                    sb.Append(" m³");
                }
                else
                {
                    AppendNumber(sb, volumeM3 * 1000);
                    sb.Append(" L");
                }
            }

            public static void AppendPower(StringBuilder sb, double powerWatts)
            {
                if (Math.Abs(powerWatts) >= 1000000)
                {
                    AppendNumber(sb, powerWatts / 1000000);
                    sb.Append(" MW");
                }
                else if (Math.Abs(powerWatts) >= 1000)
                {
                    AppendNumber(sb, powerWatts / 1000);
                    sb.Append(" kW");
                }
                else
                {
                    AppendNumber(sb, powerWatts);
                    sb.Append(" W");
                }
            }

            public static void AppendDistance(StringBuilder sb, double distanceMeters)
            {
                if (Math.Abs(distanceMeters) >= 1000000)
                {
                    AppendNumber(sb, distanceMeters / 1000000);
                    sb.Append(" Mm");
                }
                else if (Math.Abs(distanceMeters) >= 1000)
                {
                    AppendNumber(sb, distanceMeters / 1000);
                    sb.Append(" km");
                }
                else
                {
                    AppendNumber(sb, distanceMeters);
                    sb.Append(" m");
                }
            }

            public static void AppendNumber(StringBuilder sb, double d)
            {
                if (Math.Abs(d) >= 1000000)
                {
                    sb.AppendFormat("{0:F1}", d / 1000000).Append("M");
                }
                else if (Math.Abs(d) >= 1000)
                {
                    sb.AppendFormat("{0:F1}", d / 1000).Append("k");
                }
                else
                {
                    sb.AppendFormat("{0:F1}", d);
                }
            }
        }
    }
}
