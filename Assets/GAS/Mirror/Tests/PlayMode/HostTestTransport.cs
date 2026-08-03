using System;
using Mirror;

namespace GAS.Mirror.Tests
{
    internal sealed class HostTestTransport :
        Transport
    {
        private const int k_MaxPacketSize = 64 * 1024;

        public override bool Available()
        {
            return true;
        }

        public override bool ClientConnected()
        {
            return NetworkClient.active;
        }

        public override void ClientConnect(
            string address)
        {
            throw new NotSupportedException(
                "Host test transport does not support remote connections.");
        }

        public override void ClientSend(
            ArraySegment<byte> segment,
            int channelId = Channels.Reliable)
        {
            throw new NotSupportedException(
                "Host test transport does not send remote packets.");
        }

        public override void ClientDisconnect()
        {
        }

        public override Uri ServerUri()
        {
            return new Uri(
                "host-test://localhost");
        }

        public override bool ServerActive()
        {
            return NetworkServer.active;
        }

        public override void ServerStart()
        {
        }

        public override void ServerSend(
            int connectionId,
            ArraySegment<byte> segment,
            int channelId = Channels.Reliable)
        {
            throw new NotSupportedException(
                "Host test transport does not send remote packets.");
        }

        public override void ServerDisconnect(
            int connectionId)
        {
        }

        public override string ServerGetClientAddress(
            int connectionId)
        {
            return "localhost";
        }

        public override void ServerStop()
        {
        }

        public override int GetMaxPacketSize(
            int channelId = Channels.Reliable)
        {
            return k_MaxPacketSize;
        }

        public override void Shutdown()
        {
        }
    }
}