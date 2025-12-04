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
        public class Rotor
        {
            public bool IsInverted { get; private set; }
            public float MinAngle
            {
                get
                {
                    float minAngle;
                    if (IsInverted)
                    {
                        minAngle = RotorBlock.LowerLimitRad;
                    }
                    else
                    {
                        minAngle = -RotorBlock.UpperLimitRad;
                    }
                    return minAngle;
                }
                set
                {
                    if (IsInverted)
                    {
                        RotorBlock.LowerLimitRad = value;
                    }
                    else
                    {
                        RotorBlock.UpperLimitRad = -value;
                    }
                }
            }
            public float MaxAngle
            {
                get
                {
                    float maxAngle;
                    if (IsInverted)
                    {
                        maxAngle = RotorBlock.UpperLimitRad;
                    }
                    else
                    {
                        maxAngle = -RotorBlock.LowerLimitRad;
                    }
                    return maxAngle;
                }
                set
                {
                    if (IsInverted)
                    {
                        RotorBlock.UpperLimitRad = value;
                    }
                    else
                    {
                        RotorBlock.LowerLimitRad = -value;
                    }
                }
            }
            public float Range => MaxAngle - MinAngle;
            public float CurrentAngle => IsInverted ? RotorBlock.Angle : -RotorBlock.Angle;
            public float Velocity
            {
                get
                {
                    return IsInverted ? RotorBlock.TargetVelocityRad : -RotorBlock.TargetVelocityRad;
                }
                set
                {
                    RotorBlock.TargetVelocityRad = IsInverted ? value : -value;
                }
            }

            public bool IsMaxed => CurrentAngle >= MaxAngle - 0.05f;
            public bool IsMinned => CurrentAngle <= MinAngle + 0.05f;
            public bool IsSaturated
            {
                get
                {
                    if (Velocity > 0)
                    {
                        return IsMaxed;
                    }
                    else if (Velocity < 0)
                    {
                        return IsMinned;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public IMyMotorStator RotorBlock { get; private set; }
            public Rotor(string blockName)
            {
                RotorBlock = AllGridBlocks.Find(b => b is IMyMotorStator && b.CustomName.Contains(blockName)) as IMyMotorStator;
                if (RotorBlock == null)
                {
                    DebugWrite($"Error: Rotor block '{blockName}' not found!\n", true);
                    throw new ArgumentException($"Rotor block '{blockName}' not found!\n");
                }                    

                IsInverted = RotorBlock.CustomData.Contains("-Inverted");
            }

            public Rotor(IMyMotorStator rotorBlock)
            {
                if (rotorBlock == null)
                {
                    DebugWrite($"Error: Rotor block is null!\n", true);
                    throw new ArgumentException($"Rotor block is null!\n");
                }
                    
                RotorBlock = rotorBlock;
                IsInverted = RotorBlock.CustomData.Contains("-Inverted");
            }
        }
    }
}
