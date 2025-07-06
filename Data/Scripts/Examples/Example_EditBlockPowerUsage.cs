using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    // MyObjectBuilder_OreDetector would be the block type, the suffix can be found in TypeId in the block definition.
    // No subtypes defined, it will attach to all subtypes of that type.
    // To define specific subtypes, see this format:
    //    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "Subtype here", "More subtypes if needed", "etc")]
    //
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false)]
    public class Example_EditPowerUsage : MyGameLogicComponent
    {
        const float POWER_REQUIRED_MW = 10.0f;

        private IMyFunctionalBlock Block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // WARNING: this cast will fail and prevent the block from spawning if the block does not have the on/off capability.
            // Cockpits/cryo/RC can use power but can't be turned off, for example.
            // If you do have such a block, replace IMyFunctionalBlock with IMyCubeBlock in both places and remove the Block.Enabled condition in the power method.
            Block = (IMyFunctionalBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            var sink = Entity.Components.Get<MyResourceSinkComponent>();

            if(sink != null)
            {
                sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, ComputePowerRequired);
                sink.Update();
            }
        }

        private float ComputePowerRequired()
        {
            if(!Block.Enabled || !Block.IsFunctional)
                return 0f;

            // You can of course add some more complicated logic here.
            // However you need to call sink.Update() whenever you think you need the power to update.
            // Updating sink will call sink.SetRequiredInputByType(<ThisMethod>) for every resource type.
            // One way to keep it topped up at a reasonable rate is to use Update100.
            // The game will call Update() when it feels like it too so do some tests.

            return POWER_REQUIRED_MW;
        }
    }
}