using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    // This object gets attached to entities depending on their type and optionally subtype aswell.
    // The 2nd arg, "false", is for entity-attached update if set to true which is not recommended, see for more info: https://forum.keenswh.com/threads/modapi-changes-jan-26.7392280/
    // Remove any method that you don't need, they're only there to show what you can use, and also remove comments you've read as they're only for example purposes and don't make sense in a final mod.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "SubTypeIdHere", "more if needed...")]
    public class Example_GameLogic : MyGameLogicComponent
    {
        private IMyCubeBlock block; // storing the entity as a block reference to avoid re-casting it every time it's needed, this is the lowest type a block entity can be.

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // this method is called async! always do stuff in the first update unless you're sure it must be in this one.
            // NOTE the objectBuilder arg is not the Entity's but the component's, and since the component wasn't loaded from an OB that means it's always null, which it is (AFAIK).

            block = (IMyCubeBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME; // allow UpdateOnceBeforeFrame() to execute, remove if not needed
        }

        public override void UpdateOnceBeforeFrame()
        {
            // first update, remove if not needed

            if(block.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            // do stuff

            // you can access stuff from session via Example_Session.Instance.[...]

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME; // allow UpdateAfterSimulation() and UpdateAfterSimulation100() to execute, remove if not needed
        }

        public override void MarkForClose()
        {
            // called when entity is about to be removed for whatever reason (block destroyed, entity deleted, ship despawn because of sync range, etc)
        }

        public override void UpdateBeforeSimulation()
        {
            // executed 60 times a second after physics simulation, unless game is paused.
            // triggered only if NeedsUpdate contains MyEntityUpdateEnum.EACH_FRAME.
        }

        public override void UpdateAfterSimulation()
        {
            // executed 60 times a second before physics simulation, unless game is paused.
            // triggered only if NeedsUpdate contains MyEntityUpdateEnum.EACH_FRAME.
        }

        public override void UpdateAfterSimulation100()
        {
            // executed approximately every 100 ticks (~1.66s), unless game is paused.
            // why approximately? Explained at the "Important information" in: https://forum.keenswh.com/threads/pb-scripting-guide-how-to-use-self-updating.7398267/
            // there's also a 10-tick variant.
            // triggered only if NeedsUpdate contains MyEntityUpdateEnum.EACH_100TH_FRAME, same for UpdateBeforeSimulation100().
        }
    }
}