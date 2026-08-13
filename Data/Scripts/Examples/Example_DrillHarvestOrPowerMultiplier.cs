using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    // No subtypes defined, it will attach to ALL blocks of that type.
    // To define specific subtypes, see this format:
    //    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Drill), false, "Subtype here", "More subtypes if needed", "etc")]
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Drill), false)]
    public class Example_DrillHarvestOrPowerMultiplier : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            var drill = (IMyShipDrill)Entity;
            drill.DrillHarvestMultiplier = 0.5f;
            //drill.PowerConsumptionMultiplier = 2.0f;
        }
    }
}