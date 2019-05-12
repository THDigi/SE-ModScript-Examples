using ProtoBuf;
using Sandbox.ModAPI;

namespace Digi.Example_NetworkProtobuf
{
    // tag numbers in ProtoInclude collide with numbers from ProtoMember in the same class, therefore they must be unique.
    [ProtoInclude(1000, typeof(PacketSimpleExample))]
    [ProtoContract]
    public abstract class PacketBase
    {
        // this field's value will be sent if it's not the default value.
        // to define a default value you must use the [DefaultValue(...)] attribute.
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
