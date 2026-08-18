using System;
using System.Collections.Generic;

namespace Network
{
    /// <summary>
    /// 测试替身：记录型假传输，不碰真实网络。
    /// - 手动触发事件（SimulateXxx）模拟"网络发生了什么"
    /// - 记录所有发送（Sent 列表）供测试断言"服务器发出去什么"
    /// OnMessageReceived(senderId, msg) 的 senderId 语义与真实传输一致。
    /// </summary>
    public class FakeTransport : INetworkTransport
    {
        // 记录服务器发出去的消息（供测试断言）
        public readonly List<IMessage> Sent = new List<IMessage>();

        public bool IsServer { get; private set; }
        public bool IsConnected { get; set; }

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<ulong, IMessage> OnMessageReceived;
        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;

        public void StartServer(ushort port)
        {
            IsServer = true;
            IsConnected = true;
        }

        public void ConnectToServer(string ip, ushort port)
        {
            IsServer = false;
            IsConnected = true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public void Pump() { }

        public void SendToServer(IMessage msg) => Sent.Add(msg);
        public void BroadcastToClients(IMessage msg) => Sent.Add(msg);
        public void SendToClient(ulong clientId, IMessage msg) => Sent.Add(msg);

        // ---- 模拟事件（测试驱动用）----
        public void SimulateOnConnected() => OnConnected?.Invoke();
        public void SimulateOnDisconnected() => OnDisconnected?.Invoke();
        public void SimulateOnMessageReceived(ulong senderId, IMessage msg) => OnMessageReceived?.Invoke(senderId, msg);
        public void SimulateClientConnected(ulong clientId) => OnClientConnected?.Invoke(clientId);
        public void SimulateClientDisconnected(ulong clientId) => OnClientDisconnected?.Invoke(clientId);

        // ---- 测试辅助：从 Sent 里找某类型的消息 ----
        public T FindSent<T>() where T : class
        {
            foreach (var m in Sent)
                if (m is T t) return t;
            return null;
        }
    }
}
