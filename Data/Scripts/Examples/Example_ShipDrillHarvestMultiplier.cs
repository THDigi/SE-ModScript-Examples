using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Drill), false, "Yoursubtypes", "or more", "add as many as you want")]
    public class ShipDrillHarvestMultiplier : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            var block = (IMyShipDrill)Entity;
            block.DrillHarvestMultiplier = 2.5f;
        }
    }
}