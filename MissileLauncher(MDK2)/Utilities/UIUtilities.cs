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
                switch (direction)
                {
                    case Direction.Left:
                        next = source.Where(element => element.Pos.X < current.Pos.X - epsilon).OrderBy(element =>
                        {
                            float dx = Math.Abs(element.Pos.X - current.Pos.X);
                            float dy = Math.Abs(element.Pos.Y - current.Pos.Y);
                            return dx + 10 * dy;
                        }).FirstOrDefault();
                        break;
                    case Direction.Right:
                        next = source.Where(element => element.Pos.X > current.Pos.X + epsilon).OrderBy(element =>
                        {
                            float dx = Math.Abs(element.Pos.X - current.Pos.X);
                            float dy = Math.Abs(element.Pos.Y - current.Pos.Y);
                            return dx + 10 * dy;
                        }).FirstOrDefault();
                        break;
                    case Direction.Up:
                        next = source.Where(element => element.Pos.Y < current.Pos.Y - epsilon).OrderBy(element =>
                        {
                            float dx = Math.Abs(element.Pos.X - current.Pos.X);
                            float dy = Math.Abs(element.Pos.Y - current.Pos.Y);
                            return 10 * dx + dy;
                        }).FirstOrDefault();
                        break;
                    case Direction.Down:
                        next = source.Where(element => element.Pos.Y > current.Pos.Y + epsilon).OrderBy(element =>
                        {
                            float dx = Math.Abs(element.Pos.X - current.Pos.X);
                            float dy = Math.Abs(element.Pos.Y - current.Pos.Y);
                            return 10 * dx + dy;
                        }).FirstOrDefault();
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
                switch (direction)
                {
                    case Direction.Left:
                        nextKey = source.Keys.Where(k => source[k].Pos.X < source[currentKey].Pos.X - epsilon).OrderBy(k =>
                        {
                            float dx = Math.Abs(source[k].Pos.X - source[currentKey].Pos.X);
                            float dy = Math.Abs(source[k].Pos.Y - source[currentKey].Pos.Y);
                            return dx + 10 * dy;
                        }).FirstOrDefault();
                        break;
                    case Direction.Right:
                        nextKey = source.Keys.Where(k => source[k].Pos.X > source[currentKey].Pos.X + epsilon).OrderBy(k =>
                        {
                            float dx = Math.Abs(source[k].Pos.X - source[currentKey].Pos.X);
                            float dy = Math.Abs(source[k].Pos.Y - source[currentKey].Pos.Y);
                            return dx + 10 * dy;
                        }).FirstOrDefault();
                        break;
                    case Direction.Up:
                        nextKey = source.Keys.Where(k => source[k].Pos.Y < source[currentKey].Pos.Y - epsilon).OrderBy(k =>
                        {
                            float dx = Math.Abs(source[k].Pos.X - source[currentKey].Pos.X);
                            float dy = Math.Abs(source[k].Pos.Y - source[currentKey].Pos.Y);
                            return 10 * dx + dy;
                        }).FirstOrDefault();
                        break;
                    case Direction.Down:
                        nextKey = source.Keys.Where(k => source[k].Pos.Y > source[currentKey].Pos.Y + epsilon).OrderBy(k =>
                        {
                            float dx = Math.Abs(source[k].Pos.X - source[currentKey].Pos.X);
                            float dy = Math.Abs(source[k].Pos.Y - source[currentKey].Pos.Y);
                            return 10 * dx + dy;
                        }).FirstOrDefault();
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
        }
    }
}
