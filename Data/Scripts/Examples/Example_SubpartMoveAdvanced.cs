using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples.SubpartMoveAdvanced
{
    // A more flexible code example for moving and rotating various subparts, can be relatively easily expanded with custom code.

    // The MyEntityComponentDescriptor line is the block type and subtype(s) to attach this logic to.
    //  - for type use the <TypeId> and add MyObjectBuilder_ prefix.
    //  - the subtypes can be removed entirely if you want it to affect all blocks of that type.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "YourSubtypeHere", "More if needed", "etc...")]
    public class Example_SubpartMoveAdvanced : MyGameLogicComponent
    {
        const float MaxDistance = 500; // camera distance limit for updating subparts on this block

        IMyCubeBlock Block;
        List<SubpartLogicBase> Logics;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if(MyAPIGateway.Session.IsServer && MyAPIGateway.Utilities.IsDedicated)
                return; // do nothing DS-side

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void MarkForClose()
        {
            try
            {
                if(Logics != null)
                {
                    foreach(var logic in Logics)
                    {
                        logic.Dispose();
                    }
                    Logics.Clear();
                }
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                Block = (IMyCubeBlock)Entity;
                if(Block.CubeGrid?.Physics == null)
                    return; // skip ghost/projected grids

                Logics = new List<SubpartLogicBase>();

                // example linear smooth movement
                Logics.Add(new SubpartMove(Block,
                    // must be without "subpart_" prefix but exact letter case.
                    // can have a deeper path separated by / for example "Base1/Base2" will go through subpart_Base1 to find subpart_Base2 inside it which it will use.
                    subpartPath: "Thing",

                    frequency: 0.5f, // how many full movements per second
                    rampUp: 3.0f, // seconds it takes to reach full frequency
                    rampDown: 5.0f, // seconds it takes from full frequency to full stop
                    axis: new Vector3(0.0f, 1.0f, 0.0f), // direction to move relative to subpart, X/Y/Z = right/up/back. gets normalized automatically.
                    length: 2.5f, // how far to move on the axis, in meters
                    offsetTime: 0.0f // offset animation time, for example useful on engine pistons animating at different positions.
                ));

                // example rotational smooth movement
                Logics.Add(new SubpartSpin(Block,
                    // must be without "subpart_" prefix but exact letter case.
                    // can have a deeper path separated by / for example "Base1/Base2" will go through subpart_Base1 to find subpart_Base2 inside it which it will use.
                    subpartPath: "Thing",

                    targetSpeed: 360.0f, // target speed in degrees per second.
                    rampUp: 5.0f, // seconds it takes to get up to full speed.
                    rampDown: 15.0f, // seconds it takes from full speed to reach full stop.
                    axis: new Vector3(0.0f, 1.0f, 0.0f) // rotation axis relative to subpart, X/Y/Z = right/up/back. gets normalized automatically.
                ));

                // can add more like either of the above for more subparts to be affected, can comment out either too.


                if(Logics != null && Logics.Count > 0)
                {
                    foreach(var logic in Logics)
                    {
                        logic.PostInit();
                    }

                    NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                }
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if(Logics == null || Vector3D.DistanceSquared(MyAPIGateway.Session.Camera.Position, Block.GetPosition()) > (MaxDistance * MaxDistance))
                    return;

                bool working = Block.IsWorking; // if block is functional and enabled and powered.

                foreach(var logic in Logics)
                {
                    logic.Update(working);
                }
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
            }
        }
    }

    public abstract class SubpartLogicBase
    {
        readonly MyCubeBlock Block;
        readonly string[] SubpartPath;

        protected IMyEntity Subpart;
        protected bool SubpartFound = false; // retrieving the matrix only once to maintain orientations on repaint and etc where subpart respawns
        protected Matrix SubpartMatrix;

        static readonly char[] Separator = new char[] { '/' };

        public SubpartLogicBase(IMyCubeBlock block, string subpartPath)
        {
            Block = (MyCubeBlock)block;
            SubpartPath = subpartPath.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        }

        public void PostInit()
        {
            Block.OnModelRefresh += BlockModelChanged;
            BlockModelChanged(Block);
        }

        public virtual void Dispose()
        {
            Block.OnModelRefresh -= BlockModelChanged;
        }

        void BlockModelChanged(MyEntity ent)
        {
            if(!Block.IsBuilt)
            {
                // reset subparts on construction stage
                SubpartFound = false;
            }

            Subpart = GetSubpart(SubpartPath);
            if(Subpart == null)
                return; // don't log errors, it could be in construction stage

            if(!SubpartFound)
            {
                SubpartFound = true;
                SubpartMatrix = Subpart.PositionComp.LocalMatrixRef;
                OnFirstRetrieve();
            }
            else
            {
                // override with last stored matrix so it persists orientation between paints or build stages
                Subpart.PositionComp.SetLocalMatrix(ref SubpartMatrix);
            }
        }

        protected virtual void OnFirstRetrieve() { }

        public abstract void Update(bool working);

        IMyEntity GetSubpart(params string[] path)
        {
            IMyEntity result = Block;

            if(path != null)
            {
                foreach(string name in path)
                {
                    MyEntitySubpart subpart;
                    if(result.TryGetSubpart(name, out subpart))
                    {
                        result = subpart;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return result;
        }
    }

    public class SubpartSpin : SubpartLogicBase
    {
        readonly float RadPerTick;
        readonly float RampUpPerTick;
        readonly float RampDownPerTick;
        readonly Vector3 Axis;

        float TargetSpeedRatio; // used for smooth transition
        int NormalizeTimer = NormalizeCooldownTicks;
        const int NormalizeCooldownTicks = 60 * 10;

        public SubpartSpin(IMyCubeBlock block, string subpartPath,
            float targetSpeed, float rampUp, float rampDown, Vector3? axis = null)
            : base(block, subpartPath)
        {
            const float TicksPerSecond = 60f;
            RadPerTick = MathHelper.ToRadians(targetSpeed / TicksPerSecond);
            RampUpPerTick = ((1f / rampUp) / TicksPerSecond);
            RampDownPerTick = ((1f / rampDown) / TicksPerSecond);

            Axis = (axis.HasValue ? Vector3.Normalize(axis.Value) : Vector3.Forward);
        }

        //public override void Dispose()
        //{
        //    base.Dispose(); // must call this!
        //
        //    // run your disposing code
        //}

        public override void Update(bool working)
        {
            if(!working && Math.Abs(TargetSpeedRatio) < 0.00001f)
                return;

            if(working && TargetSpeedRatio < 1)
            {
                TargetSpeedRatio = Math.Min(TargetSpeedRatio + RampUpPerTick, 1);
            }
            else if(!working && TargetSpeedRatio > 0)
            {
                TargetSpeedRatio = Math.Max(TargetSpeedRatio - RampDownPerTick, 0);
            }

            if(Subpart != null && TargetSpeedRatio > 0)
            {
                SubpartMatrix = Matrix.CreateFromAxisAngle(Axis, TargetSpeedRatio * RadPerTick) * SubpartMatrix;
                SubpartMatrix.Translation = Subpart.LocalMatrix.Translation; // maintain position if changed by other things

                if(--NormalizeTimer <= 0)
                {
                    NormalizeTimer = NormalizeCooldownTicks;

                    // normalize to avoid any rotation inaccuracies over time resulting in weird scaling
                    SubpartMatrix = Matrix.Normalize(SubpartMatrix);
                }

                Subpart.PositionComp.SetLocalMatrix(ref SubpartMatrix);
            }
        }
    }

    public class SubpartMove : SubpartLogicBase
    {
        readonly float Frequency;
        readonly float RampUpPerTick;
        readonly float RampDownPerTick;
        readonly float Length;
        readonly float OffsetTime;
        readonly Vector3 Axis;

        float TargetSpeedRatio;
        Vector3D OriginalPosition;
        float TimeTicks;

        public SubpartMove(IMyCubeBlock block, string subpartPath,
            float frequency, float rampUp, float rampDown, float length, float offsetTime = 0, Vector3? axis = null)
            : base(block, subpartPath)
        {
            const float TicksPerSecond = 60f;
            Frequency = frequency;
            RampUpPerTick = ((1f / rampUp) / TicksPerSecond);
            RampDownPerTick = ((1f / rampDown) / TicksPerSecond);
            Length = length;
            OffsetTime = offsetTime;

            Axis = (axis.HasValue ? Vector3.Normalize(axis.Value) : Vector3.Forward);
        }

        //public override void Dispose()
        //{
        //    base.Dispose(); // must call this!
        //
        //    // run your disposing code
        //}

        protected override void OnFirstRetrieve()
        {
            base.OnFirstRetrieve();
            OriginalPosition = SubpartMatrix.Translation;
        }

        public override void Update(bool working)
        {
            if(!working && Math.Abs(TargetSpeedRatio) < 0.00001f)
                return;

            if(working && TargetSpeedRatio < 1)
            {
                TargetSpeedRatio = Math.Min(TargetSpeedRatio + RampUpPerTick, 1);
            }
            else if(!working && TargetSpeedRatio > 0)
            {
                TargetSpeedRatio = Math.Max(TargetSpeedRatio - RampDownPerTick, 0);
            }

            if(TargetSpeedRatio > 0)
            {
                TimeTicks += TargetSpeedRatio;

                if(Subpart != null)
                {
                    float time = TimeTicks / 60f; // MyAPIGateway.Session.GameplayFrameCounter / 60f;
                    float freq = Frequency;
                    float sin = (float)Math.Sin(MathHelper.TwoPi * (OffsetTime + time) * freq);
                    float ratio = (sin + 1) * 0.5f;

                    Matrix subpartMatrix = Subpart.PositionComp.LocalMatrixRef;
                    subpartMatrix.Translation = OriginalPosition + Axis * (ratio * Length);
                    Subpart.PositionComp.SetLocalMatrix(ref subpartMatrix);
                }
            }
        }
    }

    public class SimpleLog
    {
        public static void Error(object source, Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {source?.GetType()?.FullName}: {e.ToString()}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {source?.GetType()?.FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }

        public static void Error(object source, string text)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {source?.GetType()?.FullName}: {text}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {source?.GetType()?.FullName}: {text} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}
