using System;

namespace Network
{
    public class NgoAdapter : INetworkTransport
    {
        public bool IsServer { get; }
        public bool IsConnected { get; }
        
        public void StartServer(ushort port)
        {
            throw new NotImplementedException();
        }

        public void ConnectToServer(string ip, ushort port)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void Pump()
        {
            throw new NotImplementedException();
        }

        public void SendToServer(IMessage msg)
        {
            throw new NotImplementedException();
        }

        public void BroadcastToClients(IMessage msg)
        {
            throw new NotImplementedException();
        }

        public void SendToClient(ulong clientId, IMessage msg)
        {
            throw new NotImplementedException();
        }

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<IMessage> OnMessageReceived;
        
        
        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;
    }
}