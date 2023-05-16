using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Examples
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "YourSubtypeHere", "More if needed")]
    public class Example_GasOutput : MyGameLogicComponent
    {
        IMyFunctionalBlock Block;

        MyResourceSourceComponent SourceComp;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Block = (IMyFunctionalBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            SourceComp = new MyResourceSourceComponent();

            // ResourceDistributionGroups.sbc at the bottom for sources
            SourceComp.Init(MyStringHash.GetOrCompute("Reactors"), new MyResourceSourceInfo()
            {
                DefinedOutput = 0,
                ProductionToCapacityMultiplier = 1f,
                ResourceTypeId = MyResourceDistributorComponent.HydrogenId,
                IsInfiniteCapacity = true, // ignore the capacity aspect
            });

            Block.Components.Add(SourceComp);
        }

        public override void UpdateOnceBeforeFrame()
        {
            if(Block?.CubeGrid?.Physics == null)
                return; // ignore ghost grids

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            float output = 0f;

            if(Block.IsWorking && Block.Enabled)
            {
                output = 10f; // m3/s
            }

            SourceComp.Enabled = (output > 0);
            SourceComp.SetMaxOutputByType(MyResourceDistributorComponent.HydrogenId, output);
        }
    }
}