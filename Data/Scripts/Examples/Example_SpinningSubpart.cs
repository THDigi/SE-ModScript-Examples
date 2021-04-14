using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    // Edit the block type and subtypes to match your custom block.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "YourSubtypeHere", "More if needed")]
    public class Example_SpinningSubpart : MyGameLogicComponent
    {
        private const string SUBPART_NAME = "thing"; // dummy name without the "subpart_" prefix
        private const float DEGREES_PER_TICK = 1.5f; // rotation per tick in degrees (60 ticks per second)
        private const float ACCELERATE_PERCENT_PER_TICK = 0.05f; // aceleration percent of "DEGREES_PER_TICK" per tick.
        private const float DEACCELERATE_PERCENT_PER_TICK = 0.01f; // deaccleration percent of "DEGREES_PER_TICK" per tick.
        private readonly Vector3 ROTATION_AXIS = Vector3.Forward; // rotation axis for the subpart, you can do new Vector3(0.0f, 0.0f, 0.0f) for custom values
        private const float MAX_DISTANCE_SQ = 1000 * 1000; // player camera must be under this distance (squared) to see the subpart spinning

        private IMyFunctionalBlock block;
        private bool subpartFirstFind = true;
        private Matrix subpartLocalMatrix; // keeping the matrix here because subparts are being re-created on paint, resetting their orientations
        private float targetSpeedMultiplier; // used for smooth transition

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if(MyAPIGateway.Utilities.IsDedicated)
                return;

            block = (IMyFunctionalBlock)Entity;

            if(block.CubeGrid?.Physics == null)
                return;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateBeforeSimulation()
        {
            try
            {
                bool shouldSpin = block.IsWorking; // if block is functional and enabled and powered.

                if(!shouldSpin && Math.Abs(targetSpeedMultiplier) < 0.00001f)
                    return;

                if(shouldSpin && targetSpeedMultiplier < 1)
                {
                    targetSpeedMultiplier = Math.Min(targetSpeedMultiplier + ACCELERATE_PERCENT_PER_TICK, 1);
                }
                else if(!shouldSpin && targetSpeedMultiplier > 0)
                {
                    targetSpeedMultiplier = Math.Max(targetSpeedMultiplier - DEACCELERATE_PERCENT_PER_TICK, 0);
                }

                var camPos = MyAPIGateway.Session.Camera.WorldMatrix.Translation; // local machine camera position

                if(Vector3D.DistanceSquared(camPos, block.GetPosition()) > MAX_DISTANCE_SQ)
                    return;

                MyEntitySubpart subpart;
                if(Entity.TryGetSubpart(SUBPART_NAME, out subpart)) // subpart does not exist when block is in build stage
                {
                    if(subpartFirstFind) // first time the subpart was found
                    {
                        subpartFirstFind = false;
                        subpartLocalMatrix = subpart.PositionComp.LocalMatrixRef;
                    }

                    if(targetSpeedMultiplier > 0)
                    {
                        subpartLocalMatrix *= Matrix.CreateFromAxisAngle(ROTATION_AXIS, MathHelper.ToRadians(targetSpeedMultiplier * DEGREES_PER_TICK));
                        subpartLocalMatrix = Matrix.Normalize(subpartLocalMatrix); // normalize to avoid any rotation inaccuracies over time resulting in weird scaling
                    }

                    subpart.PositionComp.SetLocalMatrix(ref subpartLocalMatrix);
                }
            }
            catch(Exception e)
            {
                AddToLog(e);
            }
        }

        private void AddToLog(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {GetType().FullName}: {e.ToString()}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}
