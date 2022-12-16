using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    // this applies to all survival kits.
    // if you want to limit it to specific subtypes, after false you can add a comma and list the subtypes separated by comma, with quotes.
    // example: 
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_SurvivalKit), false, "subtype here", "and more...", "etc...")]
    // you can also have new lines after each comma if you have too many subtypes to comfortably fit on one line


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SurvivalKit), false)]
    public class Example_SurvivalKitDisableRespawn : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            Entity.Components.Remove<MyEntityRespawnComponentBase>();

            IMyTerminalBlock tb = Entity as IMyTerminalBlock;
            if(tb != null)
            {
                tb.AppendingCustomInfo += AppendCustomInfo;
                tb.RefreshCustomInfo();
            }
        }

        void AppendCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            info.AppendLine("NOTE: Cannot respawn on this block.");
        }
    }
}
