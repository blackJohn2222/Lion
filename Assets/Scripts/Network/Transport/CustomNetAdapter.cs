using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Network
{
    public class CustomNetAdapter : INetworkTransport
    {
        private NetworkDriver _driver;
        private NetworkPipeline _reliablePipeline;
        private NetworkPipeline _unreliablePipeline;
        private NetworkConnection _networkConnection;
        private ulong _nextClientId = 1;
        private Dictionary<ulong, NetworkConnection> _clients = new();
        
        private bool _isServer;
        private bool _connected; // 服务器就绪\客户端连接
        
        public bool IsServer => _isServer;
        public bool IsConnected => _connected;
                
        //共同事件
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<ulong, IMessage> OnMessageReceived;
        //服务器事件
        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;
        
        //------------配置------------
        public void StartServer(ushort port)   //服务器
        {
            _driver = NetworkDriver.Create();
            _reliablePipeline = _driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            _unreliablePipeline = NetworkPipeline.Null;
            _driver.Bind(NetworkEndpoint.AnyIpv4.WithPort(port));
            _driver.Listen();
            _isServer = true;
            _connected = true;
            OnConnected?.Invoke();
        }

        public void ConnectToServer(string ip, ushort port)   //客户端
        {
            _driver = NetworkDriver.Create();
            _reliablePipeline = _driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            _unreliablePipeline = NetworkPipeline.Null;
            _isServer = false;
            _networkConnection = _driver.Connect(NetworkEndpoint.Parse(ip, port));
        }

        public void Disconnect()
        {
            if (!_isServer && _networkConnection.IsCreated)
            {
                _driver.Disconnect(_networkConnection);
            }
            
            _driver.Dispose();
            _isServer = false;
            _connected = false;
            OnDisconnected?.Invoke();
        }

        public void Pump()
        {
            _driver.ScheduleUpdate().Complete();

            NetworkEvent.Type evt;
            while ((evt = _driver.PopEvent(out var connection, out var reader,out var pipeline)) != NetworkEvent.Type.Empty)
            {
                switch (evt)
                {
                    case NetworkEvent.Type.Connect:
                        if (_isServer)
                        {
                            ulong id = _nextClientId;
                            _nextClientId++;
                            _clients[id] =  connection;
                            OnClientConnected?.Invoke(id);
                        }
                        else
                        {
                            _connected = true;
                            OnConnected?.Invoke();
                        }
                        break;
                    case NetworkEvent.Type.Disconnect:
                        if (_isServer)
                        {
                            foreach (var pair in _clients)
                            {
                                if (pair.Value == connection)
                                {
                                    _clients.Remove(pair.Key);
                                    OnClientDisconnected?.Invoke(pair.Key);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            _connected = false;
                            OnDisconnected?.Invoke();
                        }
                        break;
                    case NetworkEvent.Type.Data:
                        int length = reader.Length - reader.GetBytesRead();
                        var nativeArray = new NativeArray<byte>(length, Allocator.Temp);
                        reader.ReadBytes(nativeArray);
                        var bytes = nativeArray.ToArray();
                        nativeArray.Dispose();
                        
                        IMessage msg = SerializeTool.Deserialize(bytes);

                        // 查来源：服务器查连接表，客户端收到的都来自服务器（0）
                        ulong senderId = 0;
                        if (_isServer)
                        {
                            foreach (var pair in _clients)
                            {
                                if (pair.Value == connection) { senderId = pair.Key; break; }
                            }
                        }

                        OnMessageReceived?.Invoke(senderId, msg);
                        break;
                }
            }
        }

        //-------------发送-------------
        public void SendToServer(IMessage msg)
        {
            SendOnConnection(_networkConnection,msg);
        }

        public void BroadcastToClients(IMessage msg)
        {
            foreach (var conn in _clients.Values)
            {
                SendOnConnection(conn, msg);
            }
        }

        public void SendToClient(ulong clientId, IMessage msg)
        {
            if (_clients.TryGetValue(clientId, out var conn))
            {
                SendOnConnection(conn, msg);
            }
        }

        private void SendOnConnection(NetworkConnection conn, IMessage msg)
        {
            var bytes = SerializeTool.Serialize(msg);
            var pipe = msg.Type == MessageType.PlayerStateSync ? _unreliablePipeline : _reliablePipeline;

            if (_driver.BeginSend(pipe, conn, out var writer) == 0)
            {
                writer.WriteBytes(bytes);
                _driver.EndSend(writer);
            }
        }
        
    }
}