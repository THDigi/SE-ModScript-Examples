using Digi.NetworkLib;
using ProtoBuf;

namespace Digi.Examples.NetworkProtobuf
{
    // An example packet with a string and a number.
    // Note that it must be ProtoIncluded in RegisterPackets.cs!
    [ProtoContract]
    public class PacketSimpleExample : PacketBase
    {
        public PacketSimpleExample() { } // Empty constructor required for deserialization

        // Each field has to have a unique ProtoMember number.
        //   And ideally don't change its type after mod is released, instead give it a new number and comment out the old one.

        // A protomember's value will only be sent if it's not the default value, which saves on bandwidth.
        //   WARNING: default value is not being sent and protobuf can't tell between default or null.
        //   Therefore to keep it simple, do not give fields any predetermined value.
        //   If you must have an, for example, integer with a defalut value, use nullable and use that value if it's null.

        [ProtoMember(1)]
        public string Text;

        [ProtoMember(2)]
        public int Number;

        public void Setup(string text, int number)
        {
            // Ensure you assign ALL the protomember fields here to avoid problems.
            Text = text;
            Number = number;
        }

        // Alternative way of handling the data elsewhere.
        // Or you can handle it in the Received() method below and remove this event, up to you.
        public static event ReceiveDelegate<PacketSimpleExample> OnReceive;

        public override void Received(ref PacketInfo packetInfo, ulong senderSteamId)
        {
            OnReceive?.Invoke(this, ref packetInfo, senderSteamId);
        }
    }
}
