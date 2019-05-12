using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Input;

namespace Digi.Example_NetworkProtobuf
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ExampleNetwork_Session : MySessionComponentBase
    {
        // the ID in this must be unique between other mods.
        // usually suggested to be the last few numbers of your workshopId.
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
            // then the server player/console/log will have the message you sent
            if(MyAPIGateway.Input.IsNewKeyPressed(MyKeys.L))
            {
                Networking.SendToServer(new PacketSimpleExample("L was pressed", 5000));
            }
        }
    }
}
