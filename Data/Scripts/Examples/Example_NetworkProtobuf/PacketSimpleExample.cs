using ProtoBuf;
using Sandbox.ModAPI;

namespace Digi.Example_NetworkProtobuf
{
    [ProtoContract]
    public class PacketSimpleExample : PacketBase
    {
        public PacketSimpleExample() { } // Empty constructor required for deserialization

        [ProtoMember(1)]
        private string Data;

        public PacketSimpleExample(string data)
        {
            Data = data;
        }

        public override bool Received()
        {
            MyAPIGateway.Utilities.ShowNotification($"PacketThing received, data: {Data}");

            return true; // relay packet to other clients (only works if server)
        }
    }
}
