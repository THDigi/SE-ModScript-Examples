using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Digi.AttachedLights
{
    public class BlockLogic
    {
        public readonly IMyCubeBlock Block;
        public readonly AttachedLightsSession Session;

        /// <summary>
        /// View range limiter, only used if > 0.
        /// </summary>
        public float MaxViewRange
        {
            set { maxViewRangeSq = value * value; }
        }

        private readonly LightConfigurator configurator;
        private Dictionary<string, MyLight> lights;
        private bool inViewRange = true;
        private float maxViewRangeSq;

        public BlockLogic(AttachedLightsSession session, IMyCubeBlock block, LightConfigurator configurator)
        {
            Session = session;
            Block = block;
            this.configurator = configurator;

            Block.IsWorkingChanged += WorkingChanged;

            FindLightDummies();

            WorkingChanged(Block);

            if(maxViewRangeSq > 0)
                session.ViewDistanceChecks.Add(Block.EntityId, RangeCheck);
        }

        void FindLightDummies()
        {
            var dummies = Session.TempDummies;
            dummies.Clear();
            Block.Model.GetDummies(dummies);

            foreach(var dummy in dummies.Values)
            {
                if(dummy.Name.StartsWith(AttachedLightsSession.DUMMY_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    if(lights == null)
                        lights = new Dictionary<string, MyLight>();

                    CreateLight(dummy.Name, dummy.Matrix);
                }
            }

            if(lights == null)
                SimpleLog.Error(this, $"{Block.BlockDefinition} has no dummies with '{AttachedLightsSession.DUMMY_PREFIX}' prefix!");
        }

        void CreateLight(string dummyName, Matrix dummyMatrix)
        {
            var light = MyLights.AddLight();
            light.Start(dummyName);
            light.Color = Color.White;
            light.Range = Block.CubeGrid.GridSize;
            light.Falloff = 1f;
            light.Intensity = 2f;
            light.ParentID = Block.CubeGrid.Render.GetRenderObjectID();
            light.Position = Vector3D.Transform(Vector3D.Transform(dummyMatrix.Translation, Block.WorldMatrix), Block.CubeGrid.WorldMatrixInvScaled);
            light.ReflectorDirection = Vector3D.TransformNormal(Vector3D.TransformNormal(dummyMatrix.Forward, Block.WorldMatrix), Block.CubeGrid.WorldMatrixInvScaled);
            light.ReflectorUp = Vector3D.TransformNormal(Vector3D.TransformNormal(dummyMatrix.Up, Block.WorldMatrix), Block.CubeGrid.WorldMatrixInvScaled);
            lights.Add(dummyName, light);

            configurator(dummyName, light, this);

            light.UpdateLight();
        }

        public void Close()
        {
            if(lights != null)
            {
                foreach(var light in lights.Values)
                {
                    MyLights.RemoveLight(light);
                }

                lights.Clear();
            }
        }

        void WorkingChanged(IMyCubeBlock block)
        {
            if(!inViewRange)
                return;

            SetLights(block.IsWorking);
        }

        void SetLights(bool on)
        {
            foreach(var light in lights.Values)
            {
                light.LightOn = on;
                light.GlareOn = on;

                if(light.LightType == MyLightType.SPOTLIGHT)
                    light.ReflectorOn = on;

                light.UpdateLight();
            }
        }

        void RangeCheck(Vector3D cameraPosition)
        {
            var inRange = (Vector3D.DistanceSquared(cameraPosition, Block.WorldMatrix.Translation) <= maxViewRangeSq);

            if(inViewRange == inRange)
                return;

            inViewRange = inRange;
            SetLights(inViewRange ? Block.IsWorking : false);
        }
    }
}
