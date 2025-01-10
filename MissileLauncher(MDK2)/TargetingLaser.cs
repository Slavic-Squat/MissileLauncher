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
        public class TargetingLaser
        {
            #region General Info
            private Program program;
            private int ID;
            #endregion

            #region Parts
            private IMyMotorStator azimuthRotor;
            private IMyMotorStator elevationRotor;
            private IMyShipController laserController;
            private List<IMyCameraBlock> cameraArray = new List<IMyCameraBlock>();
            #endregion

            #region Properties
            private bool azimuthRotorInverted;
            private bool elevationRotorInverted;
            private float maxTargetDistance;
            private float maxRaycastDistance;
            private float raycastDistanceGrowthSpeed;
            private float sensitivity;
            #endregion

            #region State Info
            private float azimuthRotorAngle;
            private float azimuthError;
            private float elevationRotorAngle;
            private float elevationError;
            private int raycastCounter;
            private float totalAvailRaycastDistance;
            private DateTime lastUniqueDetection;
            private int matchingDetectionCounter;
            private MyDetectedEntityInfo detectedTarget;
            private MyDetectedEntityInfo previouslyDetectedTarget;
            private bool manualOverride = false;
            #endregion

            #region Controllers
            private PIDControl azimuthPID;
            private PIDControl elevationPID;
            #endregion

            #region Output
            public MyDetectedEntityInfo lockedTarget;
            public DateTime lastTargetDetection;
            #endregion

            private MyDetectedEntityInfo emptyTarget = new MyDetectedEntityInfo();

            public TargetingLaser(Program program, int ID, IMyShipController controller, float sensitivity = 0.05f, float maxRaycastDistance = 5000, float raycastDistanceGrowthSpeed = 200)
            {
                this.program = program;
                this.ID = ID;
                this.sensitivity = sensitivity;
                this.maxRaycastDistance = maxRaycastDistance;
                this.maxTargetDistance = maxRaycastDistance * 0.8f;
                this.raycastDistanceGrowthSpeed = raycastDistanceGrowthSpeed;
                
                try
                {
                    azimuthRotor = program.GridTerminalSystem.GetBlockWithName($"Azimuth Rotor [{ID}]") as IMyMotorStator;
                    if (azimuthRotor == null)
                    {
                        throw new Exception();
                    }
                    elevationRotor = program.GridTerminalSystem.GetBlockWithName($"Elevation Rotor [{ID}]") as IMyMotorStator;
                    if (elevationRotor == null)
                    {
                        throw new Exception();
                    }
                    laserController = controller;
                    program.GridTerminalSystem.GetBlockGroupWithName($"Camera Array [{ID}]").GetBlocksOfType<IMyCameraBlock>(cameraArray);
                }
                catch (Exception ex)
                {
                    program.Echo("Error in TargetingLaser construction");
                    throw;
                }

                foreach (IMyCameraBlock camera in cameraArray)
                {
                    camera.EnableRaycast = true;
                }

                azimuthRotorInverted = azimuthRotor.CustomData.Contains("Inverted");
                elevationRotorInverted = elevationRotor.CustomData.Contains("Inverted");
                azimuthPID = new PIDControl(25, 2, 0.1f);
                elevationPID = new PIDControl(25, 2, 0.1f);
            }

            public void Run(DateTime time)
            {
                float timeDeltaMiliseconds = (float)program.Runtime.TimeSinceLastRun.TotalMilliseconds;
                float timeDeltaSeconds = (float)program.Runtime.TimeSinceLastRun.TotalSeconds;

                azimuthRotorAngle = azimuthRotorInverted ? -azimuthRotor.Angle : azimuthRotor.Angle;
                elevationRotorAngle = elevationRotorInverted ? -elevationRotor.Angle : elevationRotor.Angle;
                Matrix H0 = azimuthRotor.WorldMatrix;
                Matrix H1 = Matrix.CreateRotationY(azimuthRotorAngle);
                Matrix H2 = Matrix.CreateRotationX(elevationRotorAngle);
                H2.Translation = new Vector3(0, 3, 0);

                Matrix referenceMatrix = H2 * H1 * H0;

                /*
                Quaternion azimuthRotation = Quaternion.CreateFromAxisAngle(referenceMatrix.Up, -azimuthRotor.Angle);
                Quaternion elevationRotation = Quaternion.CreateFromAxisAngle(referenceMatrix.Right, -elevationRotor.Angle);
                Quaternion totalRotation = azimuthRotation * elevationRotation;

                Matrix.Transform(ref referenceMatrix, ref totalRotation, out referenceMatrix);

                referenceMatrix.Translation = Vector3.Transform(rotationPointLocal, azimuthRotor.WorldMatrix);
                */

                TimeSpan timeSinceLastTargetDetection = TimeSpan.Zero;
                Vector3 estimatedTargetPos = Vector3.Zero;
                float estimatedTargetDistance = 0;

                if (!lockedTarget.IsEmpty())
                {
                    timeSinceLastTargetDetection = time - lastTargetDetection;
                    estimatedTargetPos = lockedTarget.Position + lockedTarget.Velocity * (float)timeSinceLastTargetDetection.TotalSeconds;
                    estimatedTargetDistance = (estimatedTargetPos - referenceMatrix.Translation).Length();

                    if (laserController.MoveIndicator.Y == -1 || estimatedTargetDistance > maxTargetDistance || timeSinceLastTargetDetection.TotalSeconds > 5)
                    {
                        lockedTarget = emptyTarget;
                        lastTargetDetection = DateTime.MinValue;
                        matchingDetectionCounter = 0;
                    }
                }

                if (!lockedTarget.IsEmpty())
                {
                    if (manualOverride == false)
                    {
                        Vector3 estimatedTargetDirLocal = Vector3.Normalize(Vector3.TransformNormal(estimatedTargetPos - referenceMatrix.Translation, Matrix.Transpose(referenceMatrix)));
                        azimuthError = (float)Math.Atan2(-estimatedTargetDirLocal.X, -estimatedTargetDirLocal.Z);
                        elevationError = (float)Math.Asin(estimatedTargetDirLocal.Y);

                        azimuthRotor.TargetVelocityRad = azimuthRotorInverted ? -azimuthPID.Run(azimuthError, timeDeltaSeconds) : azimuthPID.Run(azimuthError, timeDeltaSeconds);
                        elevationRotor.TargetVelocityRad = elevationRotorInverted ? -elevationPID.Run(elevationError, timeDeltaSeconds) : elevationPID.Run(elevationError, timeDeltaSeconds);
                    }
                    else if (manualOverride == true)
                    {
                        elevationRotor.TargetVelocityRad = elevationRotorInverted ? laserController.RotationIndicator.X * sensitivity : -laserController.RotationIndicator.X * sensitivity;
                        azimuthRotor.TargetVelocityRad = azimuthRotorInverted ? laserController.RotationIndicator.Y * sensitivity : -laserController.RotationIndicator.Y * sensitivity;
                    }
                }

                else
                {
                    elevationRotor.TargetVelocityRad = elevationRotorInverted ? laserController.RotationIndicator.X * sensitivity : -laserController.RotationIndicator.X * sensitivity;
                    azimuthRotor.TargetVelocityRad = azimuthRotorInverted ? laserController.RotationIndicator.Y * sensitivity : -laserController.RotationIndicator.Y * sensitivity;
                }

                totalAvailRaycastDistance += 2 * timeDeltaMiliseconds * cameraArray.Count;
                float baseAvailRaycastDistance = 2 * maxRaycastDistance * cameraArray.Count;

                if (totalAvailRaycastDistance >= baseAvailRaycastDistance && ((!lockedTarget.IsEmpty() && manualOverride == false) || laserController.MoveIndicator.Y == 1))
                {
                    Vector3 cameraPos = cameraArray[raycastCounter].GetPosition();
                    Vector3 raycastTarget = Vector3.Zero;
                    float raycastDistance;

                    if (laserController.MoveIndicator.Y == 1 && (lockedTarget.IsEmpty() || manualOverride == true))
                    {
                        raycastTarget = referenceMatrix.Forward * maxTargetDistance + referenceMatrix.Translation;
                    }
                    else if (!lockedTarget.IsEmpty())
                    {
                        Vector3 raycastOvershoot = Vector3.Normalize(estimatedTargetPos - cameraPos) * (raycastDistanceGrowthSpeed * (float)timeSinceLastTargetDetection.TotalSeconds);
                        raycastTarget = estimatedTargetPos + raycastOvershoot;
                    }

                    raycastDistance = (raycastTarget - cameraPos).Length();
                    raycastTarget = raycastDistance > maxRaycastDistance ? Vector3.Normalize(raycastTarget - cameraPos) * maxRaycastDistance + cameraPos : raycastTarget;

                    if (cameraArray[raycastCounter].CanScan(raycastTarget))
                    {
                        MyDetectedEntityInfo raycastResult = cameraArray[raycastCounter].Raycast(raycastTarget);
                        totalAvailRaycastDistance -= raycastDistance;
                        raycastCounter++;
                        raycastCounter %= cameraArray.Count;

                        if (!raycastResult.IsEmpty())
                        {
                            detectedTarget = raycastResult;

                            if (!lockedTarget.IsEmpty() && detectedTarget.EntityId == lockedTarget.EntityId)
                            {
                                lastTargetDetection = time;
                                lockedTarget = detectedTarget;
                            }

                            else if (lockedTarget.IsEmpty())
                            {
                                if (detectedTarget.EntityId == previouslyDetectedTarget.EntityId)
                                {
                                    matchingDetectionCounter += 1;
                                }
                                else
                                {
                                    lastUniqueDetection = time;
                                    matchingDetectionCounter = 0;
                                }

                                previouslyDetectedTarget = detectedTarget;

                                TimeSpan timeSinceLastUniqueDetection = time - lastUniqueDetection;
                                if (timeSinceLastUniqueDetection.TotalSeconds > 2 && matchingDetectionCounter >= 3)
                                {
                                    lockedTarget = detectedTarget;
                                    lastTargetDetection = time;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
