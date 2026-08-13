using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    /*
     * Because the MyEntityComponentDescriptor requires an exact TypeId of an entity/block and does not include inherited types,
     *   in order to have one gamelogic for multiple types or multiple blocks, you can do it like shown below.
     * The class names do not matter, what matters is that they inherit a custom class instead of MyGameLogicComponent, which is where your logic resides.
     */


    // All these MyEntityComponentDescriptor work the same way as shown in GameLogic.cs.
    // Currently they're set to no subtypes which attach to all blocks of that type,
    //   but you can still specify subtypes if you wish, for details see GameLogic.cs.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false)]
    class GatlingTurretGL : SharedGameLogic { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false)]
    class MissileTurretGL : SharedGameLogic { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), false)]
    class InteriorTurretGL : SharedGameLogic { }
    // ... and as many more as you want!


    // no attribute on this one.
    public class SharedGameLogic : MyGameLogicComponent
    {
        // All the same things gamelogic supports, see GameLogic.cs for details.

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            //...
        }

        //...
    }
}
