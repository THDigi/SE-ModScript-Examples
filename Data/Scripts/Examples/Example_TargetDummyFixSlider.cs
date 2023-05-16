using System;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    // fixes Delay slider in target dummy block by limiting it on spawn between the actual limits (defined by sbc's Min/MaxRegenerationTimeInS)

    // HACK: having to use session comp to find spawning target dummy because MyObjectBuilder_TargetDummyBlock is not whitelisted.
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_TargetDummyFixSlider : MySessionComponentBase
    {
        MyConcurrentList<IMyTargetDummyBlock> Blocks = new MyConcurrentList<IMyTargetDummyBlock>();

        public override void LoadData()
        {
            // property changes synchronize so not necessary MP-client-side (also would unnecessarily spam)
            if(MyAPIGateway.Session.IsServer)
            {
                SetUpdateOrder(MyUpdateOrder.AfterSimulation);
                MyEntities.OnEntityCreate += MyEntities_OnEntityCreate;
            }
        }

        protected override void UnloadData()
        {
            MyEntities.OnEntityCreate -= MyEntities_OnEntityCreate;
        }

        void MyEntities_OnEntityCreate(MyEntity entity)
        {
            try
            {
                IMyTargetDummyBlock targetDummy = entity as IMyTargetDummyBlock;
                if(targetDummy != null)
                {
                    Blocks.Add(targetDummy);
                }
            }
            catch(Exception e)
            {
                LogError(e);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if(Blocks.Count > 0)
                {
                    foreach(IMyTargetDummyBlock block in Blocks)
                    {
                        ITerminalProperty<float> propDelay = block.GetProperty("Delay")?.AsFloat();
                        if(propDelay != null)
                        {
                            float delay = propDelay.GetValue(block);
                            float fixedDelay = MathHelper.Clamp(delay, propDelay.GetMinimum(block), propDelay.GetMaximum(block));

                            if(Math.Abs(delay - fixedDelay) > 0.0001f)
                            {
                                propDelay.SetValue(block, fixedDelay);
                            }
                        }
                    }

                    Blocks.Clear();
                }
            }
            catch(Exception e)
            {
                LogError(e);
                Blocks.Clear();
            }
        }

        void LogError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"{ModContext.ModName}: {e.Message}\n{e.StackTrace}");
            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {ModContext.ModName}: error at {GetType().Name} ]", 10000, MyFontEnum.Red);
        }
    }
}
