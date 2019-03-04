using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Digi.Example_NetworkProtobuf
{
    public class Networking
    {
        public readonly ushort PacketId;

        private readonly List<IMyPlayer> tempPlayers = new List<IMyPlayer>();

        public Networking(ushort packetId)
        {
            PacketId = packetId;
        }

        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(PacketId, ReceivedPacket);
        }

        public void Unregister()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(PacketId, ReceivedPacket);
        }

        private void ReceivedPacket(byte[] rawData) // executed when a packet is received on this machine
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(rawData);
                var relay = packet.Received();

                if(relay)
                    RelayToClients(packet, rawData);
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if(MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
            }
        }

        /// <summary>
        /// Send a packet to the server.
        /// Works from clients and server.
        /// </summary>
        /// <param name="packet"></param>
        public void SendToServer(PacketBase packet)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToServer(PacketId, bytes);
        }

        /// <summary>
        /// Send a packet to a specific player.
        /// Only works server side.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="steamId"></param>
        public void SendToPlayer(PacketBase packet, ulong steamId)
        {
            if(!MyAPIGateway.Multiplayer.IsServer)
                return;

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(PacketId, bytes, steamId);
        }

        /// <summary>
        /// Sends packet (or supplied bytes) to all players except server player and supplied packet's sender.
        /// Only works server side.
        /// </summary>
        public void RelayToClients(PacketBase packet, byte[] rawData = null)
        {
            if(!MyAPIGateway.Multiplayer.IsServer)
                return;

            if(rawData == null)
                rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);

            tempPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(tempPlayers);

            foreach(var p in tempPlayers)
            {
                if(p.SteamUserId == MyAPIGateway.Multiplayer.ServerId)
                    continue;

                if(p.SteamUserId == packet.SenderId)
                    continue;

                MyAPIGateway.Multiplayer.SendMessageTo(PacketId, rawData, p.SteamUserId);
            }

            tempPlayers.Clear();
        }
    }
}
