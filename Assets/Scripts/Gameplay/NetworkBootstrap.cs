using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    public class NetworkBootstrap : MonoBehaviour
    {
        private readonly List<INetworkTransport> _transports = new();
        private GameServer _server;
        private GameClient _client;
        public string playerName = "John";

        public void Update()
        {
            foreach (var transport in _transports)
            {
                transport.Pump();
            }
        }

        public void StartServer(ushort port)
        {
            var serverAdapter = new CustomNetAdapter();
            _server = new GameServer(serverAdapter);
            _server.Start(port);
            _transports.Add(serverAdapter);
        }

        public void StartClient(string ip, ushort port)
        {
            var clientAdapter = new CustomNetAdapter();
            _client = new GameClient(clientAdapter, playerName);
            _client.Connect(ip, port);
            _transports.Add(clientAdapter);
        }

        public void StartHost(ushort port)
        {
            StartServer(port);
            StartClient("127.0.0.1", port);
        }
    }
}
