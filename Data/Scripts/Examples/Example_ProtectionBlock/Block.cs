using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Example_ProtectionBlock
{
    // change MyObjectBuilder_BatteryBlock to the block type you're using, it must be the exact type, no inheritence.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "BlockSubtypeHere", "more if needed...")]
    public class ProtectionBlock : MyGameLogicComponent
    {
        // this method is called async! always do stuff in the first update unless you're sure it must be in Init().
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame() // first update of the block
        {
            var block = (IMyCubeBlock)Entity;

            if(block.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            ProtectionSession.Instance?.ProtectionBlocks.Add(block);
        }

        public override void Close() // called when block is removed for whatever reason (including ship despawn)
        {
            ProtectionSession.Instance?.ProtectionBlocks.Remove((IMyFunctionalBlock)Entity);
        }
    }
}
