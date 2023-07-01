using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_MessageOnJoin : MySessionComponentBase
    {
        const string From = "Nigerian Prince";
        const string Message = "Hello, I give you lots of money if you give me few money.";

        bool SeenMessage = false;

        public override void LoadData()
        {
            if(MyAPIGateway.Session.IsServer && MyAPIGateway.Utilities.IsDedicated) // DS side does not need this
                return;

            SetUpdateOrder(MyUpdateOrder.AfterSimulation);
        }

        public override void UpdateAfterSimulation()
        {
            if(!SeenMessage && MyAPIGateway.Session?.Player?.Character != null)
            {
                SeenMessage = true;
                MyAPIGateway.Utilities.ShowMessage(From, Message);

                // required delayed like this because it modifies the list that iterates components to trigger this update method, causing list modified exception.
                MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(MyUpdateOrder.NoUpdate));
            }
        }
    }
}