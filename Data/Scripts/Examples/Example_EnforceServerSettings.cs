using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_EnforceServerSettings : MySessionComponentBase
    {
        public override void LoadData()
        {
            var settings = MyAPIGateway.Session.SessionSettings;

            if(settings.AssemblerEfficiencyMultiplier != 1f)
            {
                MyLog.Default.WriteLineAndConsole($"{GetType().Name}: {nameof(settings.AssemblerEfficiencyMultiplier)} has to be 1 (automatically set to that).");
                settings.AssemblerEfficiencyMultiplier = 1f;
            }

            // etc...
        }
    }
}
