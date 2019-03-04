using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    // This object gets attached to entities depending on their type and optionally subtype aswell.
    // The 2nd arg, "false", is for entity-attached update if set to true which is not recommended, see for more info: https://forum.keenswh.com/threads/modapi-changes-jan-26.7392280/
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "SubTypeIdHere", "more if needed...")]
    public class Example_GameLogic : MyGameLogicComponent
    {
        private IMyCubeBlock block; // storing the entity as a block reference to avoid re-casting it every time it's needed

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // this method is called async! always do stuff in the first update unless you're sure it must be in this one.
            // NOTE the objectBuilder arg is not the Entity's but the component's, and since the component wasn't loaded from an OB that means it's always null, which it is (AFAIK).

            block = (IMyCubeBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME; // allow UpdateOnceBeforeFrame() to execute, remove if not needed
        }

        public override void UpdateOnceBeforeFrame()
        {
            // first update of the block, remove if not needed

            if(block.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            // do stuff

            // access stuff from session via Example_Session.Instance. ...

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME; // allow UpdateAfterSimulation() and UpdateAfterSimulation100() to execute, remove if not needed
        }

        public override void UpdateAfterSimulation()
        {
            // executed 60 times a second after physics simulation, unless game is paused
        }

        public override void UpdateBeforeSimulation()
        {
            // NOTE: this requires the proper NeedsUpdate flag.
        }

        public override void UpdateAfterSimulation100()
        {
            // executed APPROXIMATELY every 100 ticks (60 ticks in a second), unless game is paused.
            // Why approximately? Explained at the "Important information" in: https://forum.keenswh.com/threads/pb-scripting-guide-how-to-use-self-updating.7398267/
            // there's also a 10 tick variant.
        }

        public override void Close()
        {
            // called when block is removed for whatever reason (incl ship despawn, etc).
        }
    }
}