using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false)]
    public class Example_OreDetector : MyGameLogicComponent
    {
        const float POWER_REQUIRED_MW = 10.0f;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            var sink = Entity.Components.Get<MyResourceSinkComponent>();

            if(sink != null)
            {
                sink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, POWER_REQUIRED_MW);
                sink.Update();
            }
        }
    }
}