using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    // This object gets created and given to the entity type you specify in the attribute, optionally the subtype aswell.


    // It is not entirely reliable for some entity types, for example:
    //   - for grids it can not attach at all for MP clients;
    //   - characters it can sometimes not get added;
    //   - CTC, solar panels and oxygen farms overwrite their gamelogic comp so it breaks any mods trying to add to them;
    //   - and probably more...
    // Workaround for these is to use a session comp to track the entity additions&removals, storing them in a list/dictionary then update them yourself.


    // The MyEntityComponentDescriptor parameters:
    //
    // 1.  The typeof(MyObjectBuilder_BatteryBlock) represents the <TypeId>BatteryBlock</TypeId> from the SBC.
    //     Never use the OBs that end with "Definition" as those are not entities.
    //
    // 2.  Entity-controlled updates, always use false. For more info: https://forum.keenswh.com/threads/modapi-changes-jan-26.7392280/
    //
    // 3+. Subtype strings, you can add as many or few as you want.
    //     You can also remove them entirely if you want it to attach to all entities of that type regardless of subtype, like so:
    //         [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false)]


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "SubTypeIdHere", "more if needed...")]
    public class Example_GameLogic : MyGameLogicComponent
    {
        private IMyCubeBlock block; // storing the entity as a block reference to avoid re-casting it every time it's needed, this is the lowest type a block entity can be.


        // NOTE: All methods are optional, I'm just presenting the options and you can remove any you don't actually need.

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // the base methods are usually empty, except for OnAddedToContainer()'s, which has some sync stuff making it required to be called.
            base.Init(objectBuilder);

            // this method is called async! always do stuff in the first update instead.
            // unless you're sure it must be in this one (like initializing resource sink/source components would need to be here).

            // the objectBuilder arg is sometimes the serialized version of the entity.
            // it works for hand tools for example but not for blocks (probably because MyObjectBuilder_CubeBlock does not extend MyObjectBuilder_EntityBase).


            block = (IMyCubeBlock)Entity;


            // makes UpdateOnceBeforeFrame() execute.
            // this is a special flag that gets self-removed after the method is called.
            // it can be used multiple times but mind that there is overhead to setting this so avoid using it for continuous updates.
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if(block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;


            // do stuff...
            // you can access things from session via Example_Session.Instance.[...]


            // in other places (session, terminal control callbacks, TSS, etc) where you have an entity and you want to get this gamelogic, you can use:
            //   ent.GameLogic?.GetAs<Example_GameLogic>()
            // which will simply return null if it's not found.


            // allow UpdateAfterSimulation() and UpdateAfterSimulation100() to execute, remove if not needed
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void MarkForClose()
        {
            base.MarkForClose();

            // called when entity is about to be removed for whatever reason (block destroyed, entity deleted, ship despawn because of sync range, etc)
            // override Close() also works but it's a tiny bit later
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // this and UpdateBeforeSimulation() require NeedsUpdate to contain MyEntityUpdateEnum.EACH_FRAME.
            // gets executed 60 times a second after physics simulation, unless game is paused.
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            // this and UpdateBeforeSimulation100() require NeedsUpdate to contain EACH_100TH_FRAME.
            // executed approximately every 100 ticks (~1.66s), unless game is paused.
            //   why approximately? Explained at the "Important information" in: https://forum.keenswh.com/threads/pb-scripting-guide-how-to-use-self-updating.7398267/

            // there's also 10-tick variants, UpdateBeforeSimulation10() and UpdateAfterSimulation10()
            //   which require NeedsUpdate to contain EACH_10TH_FRAME
        }


        // less commonly used methods:

        public override bool IsSerialized()
        {
            // executed when the entity gets serialized (saved, blueprinted, streamed, etc) and asks all
            //   its components whether to be serialized too or not (calling GetObjectBuilder())

            // this can be used for serializing to Storage dictionary for example,
            //   and for reliability I recommend that Storage has at least one key in it before this runs (by adding yours in first update).

            // you cannot add custom OBs to the game so this should always return the base (which currently is always false).
            return base.IsSerialized();
        }

        public override void UpdatingStopped()
        {
            base.UpdatingStopped();

            // only called when game is paused.
        }

        // WARNING: OnAddedToScene() and OnRemovedFromScene() never trigger if the block has more than one gamelogic comp.
        // I advise not using these to avoid surprises down the line.
        // Reason is Entity.GameLogic turns into a MyCompositeGameLogicComponent which holds an inner list of the actual gamelogic components,
        //   but it does not override those 2 methods to pass their call along to the held components.
        //
        // Also advised to not use OnAddedToContainer() and OnBeforeRemovedFromContainer()
    }
}