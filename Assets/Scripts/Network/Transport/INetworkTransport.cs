using System;

namespace Network
{
    public interface INetworkTransport
    {
        bool IsServer { get; }
        bool IsConnected { get; }
        
        //共同事件
        event Action OnConnected;
        event Action OnDisconnected;
        event Action<ulong, IMessage> OnMessageReceived;

        //服务器事件
        event Action<ulong> OnClientConnected;
        event Action<ulong> OnClientDisconnected;        
        void StartServer(ushort port);
        void ConnectToServer(string ip, ushort port);
        
        void Disconnect();
        void Pump();

        void SendToServer(IMessage msg);
        void BroadcastToClients(IMessage msg);
        void SendToClient(ulong clientId, IMessage msg);
        
    }
}
