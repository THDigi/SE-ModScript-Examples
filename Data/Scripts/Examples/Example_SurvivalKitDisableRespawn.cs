using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SurvivalKit), false, "subtype here", "and more...", "etc...")]
    public class Example_SurvivalKitDisableRespawn : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.Components.Remove<MyEntityRespawnComponentBase>();
        }
    }
}
