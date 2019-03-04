using ProtoBuf;
using Sandbox.ModAPI;

namespace Digi.Example_NetworkProtobuf
{
    [ProtoInclude(2, typeof(PacketSimpleExample))]
    [ProtoContract]
    public abstract class PacketBase
    {
        [ProtoMember(1)]
        public readonly ulong SenderId;

        public PacketBase()
        {
            SenderId = MyAPIGateway.Multiplayer.MyId;
        }

        /// <summary>
        /// Called when this packet is received on this machine.
        /// </summary>
        /// <returns>Return true if you want the packet to be sent to other clients (only works server side)</returns>
        public abstract bool Received();
    }
}
