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
        public static class SpriteDefintions
        {
            public static readonly SpriteDefinition RadialGrid = new SpriteDefinition()
            {
                Name = "Radial_Grid",
                NativeAR = 1024f / 1024f,
                BaseScale = 1,
                MinDepthScale = 1,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition RadialGradient = new SpriteDefinition()
            {
                Name = "Radial_Gradient",
                NativeAR = 1024f / 1024f,
                BaseScale = 1,
                MinDepthScale = 1,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition Launcher = new SpriteDefinition()
            {
                Name = "Launcher",
                NativeAR = 1444f / 694f,
                BaseScale = 0.25f,
                MinDepthScale = 0,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition FriendlyMissile = new SpriteDefinition()
            {
                Name = "Missile",
                NativeAR = 512f / 443f,
                BaseScale = 0.2f,
                MinDepthScale = 0,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition HostileMissile = new SpriteDefinition()
            {
                Name = "Missile",
                NativeAR = 512f / 443f,
                BaseScale = 0.2f,
                MinDepthScale = 0,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition NeutralMissile = new SpriteDefinition()
            {
                Name = "Missile",
                NativeAR = 512f / 443f,
                BaseScale = 0.2f,
                MinDepthScale = 0,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition Selector = new SpriteDefinition()
            {
                Name = "Selector",
                NativeAR = 512f / 512f,
                BaseScale = 0.3f,
                MinDepthScale = 0,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition SpriteBase = new SpriteDefinition()
            {
                Name = "Sprite_Base",
                NativeAR = 224f / 97f,
                BaseScale = 0.1f,
                MinDepthScale = 1,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition FriendlyEntity = new SpriteDefinition()
            {
                Name = "Entity",
                NativeAR = 512f / 512f,
                BaseScale = 0.2f,
                MinDepthScale = 1,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition HostileEntity = new SpriteDefinition()
            {
                Name = "Entity",
                NativeAR = 512f / 512f,
                BaseScale = 0.2f,
                MinDepthScale = 1,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition NeutralEntity = new SpriteDefinition()
            {
                Name = "Entity",
                NativeAR = 512f / 512f,
                BaseScale = 0.2f,
                MinDepthScale = 1,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly SpriteDefinition LaserIndicator = new SpriteDefinition()
            {
                Name = "Laser_Indicator",
                NativeAR = 306f / 306f,
                BaseScale = 0.25f,
                MinDepthScale = 1,
                MaxDepthScale = 1,
                SpriteColor = Color.White,
                Rotation = 0
            };

            public static readonly VectorSpriteDefinition TargetingVector = new VectorSpriteDefinition()
            {
                Name = "SquareSimple",
                SpriteColor = Color.White
            };
        }
    }
}
