using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Input;

namespace Digi.Example_NetworkProtobuf
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Example_ProtoIncludeSession : MySessionComponentBase
    {
        public Networking Networking = new Networking(1337);

        public override void BeforeStart()
        {
            Networking.Register();
        }

        protected override void UnloadData()
        {
            Networking?.Unregister();
            Networking = null;
        }

        public override void UpdateAfterSimulation()
        {
            // example for testing ingame, press L at any point when in a world with this mod loaded
            if(MyAPIGateway.Input.IsNewKeyPressed(MyKeys.L))
            {
                Networking.SendToServer(new PacketSimpleExample("L was pressed"));
            }
        }
    }
}
