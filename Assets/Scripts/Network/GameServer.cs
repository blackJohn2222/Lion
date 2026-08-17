using System;
using System.Collections.Generic;

namespace Network
{
    public class GameServer
    {
        private readonly INetworkTransport _transport;
        private readonly Dictionary<ulong, string> _players = new();
        private readonly Dictionary<MessageType, Action<IMessage>> _handlers = new();

        public GameServer(INetworkTransport transport)
        {
            _transport = transport;
        }

        public void Start(ushort port)
        {
            _transport.OnMessageReceived += OnMessageReceived;
            _transport.OnClientDisconnected += OnClientDisconnected;
            RegisterHandlers();
            _transport.StartServer(port);
        }
        
        //---------------底层事件交互---------------
        private void OnMessageReceived(ulong senderId, IMessage msg)
        {
            msg.SenderId = senderId;
            if (_handlers.TryGetValue(msg.Type, out var handler))
            {
                handler(msg);
            }
        }

        private void OnClientDisconnected(ulong playerId)
        {
            if (_players.Remove(playerId))
            {
                var notice = new PlayerLeftNotice(playerId);
                _transport.BroadcastToClients(notice);
            }
        }
        
        //---------------注册事件---------------
        private void RegisterHandlers()
        {
            _handlers[MessageType.JoinRequest] = OnJoinRequest;
            _handlers[MessageType.ChatMessage] = OnChatMessage;
            _handlers[MessageType.PlayerStateSync] = OnPlayerStateSync;
        }
        
        //---------------事件函数---------------
        private void OnJoinRequest(IMessage message)
        {
            var req = (JoinRequest)message;
            ulong playerId = req.SenderId;
            _players[playerId] = req.PlayerName;
            
            var response = new JoinResponse(playerId, GetPlayerList());
            _transport.SendToClient(playerId, response);

            var notice = new PlayerJoinedNotice(playerId, req.PlayerName);
            _transport.BroadcastToClients(notice);
        }
        
        private void OnChatMessage(IMessage message)
        {
            var chat = (ChatMessage)message;
            if (chat.TargetId == 0)
            {
                _transport.BroadcastToClients(chat);
            }
            else
            {
                _transport.SendToClient(chat.TargetId, chat);
            }
        }
        
        private void OnPlayerStateSync(IMessage message)
        {
            _transport.BroadcastToClients(message);
        }
        
        //辅助函数
        private List<PlayerInfo> GetPlayerList()
        {
            var list = new List<PlayerInfo>();
            foreach (var pair in _players)
                list.Add(new PlayerInfo { PlayerId = pair.Key, PlayerName = pair.Value });
            return list;
        }
    }
}