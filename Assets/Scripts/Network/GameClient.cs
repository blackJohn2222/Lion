using System;
using System.Collections.Generic;

namespace Network
{
    public class GameClient
    {
        private readonly INetworkTransport _transport;                  // 传输层（构造注入）
        private readonly string _playerName;                            // 我的名字（构造传入）
        private ulong _localPlayerId;                                   // 我的 ID（JoinResponse 给的）
        private readonly Dictionary<ulong, string> _players = new();    // 我认识的玩家列表
        private readonly Dictionary<MessageType, Action<IMessage>> _handlers = new();  // 双注册表
        
        public ulong LocalPlayerId => _localPlayerId;
        public IReadOnlyDictionary<ulong, string> Players => _players;
        
        public GameClient(INetworkTransport transport, string playerName)
        {
            _transport = transport;
            _playerName = playerName;
        }
        
        public void Connect(string ip, ushort port)
        {
            _transport.OnConnected += OnConnected;                      // 订阅：连上后发加入请求
            _transport.OnMessageReceived += OnMessageReceived;          // 订阅：收消息
            RegisterHandlers();                                         // 建注册表
            _transport.ConnectToServer(ip, port);                       // 发起连接
        }
        
        //---------------底层事件交互---------------
        private void OnMessageReceived(ulong senderId, IMessage msg)
        {
            // 客户端信任服务器：不覆盖 SenderId，直接用
            if (_handlers.TryGetValue(msg.Type, out var handler))
                handler(msg);
        }
        
        private void OnConnected()
        {
            var join = new JoinRequest(_playerName);
            join.SenderId = _localPlayerId;                             // 填我的 ID（此时是 0，未分配）
            _transport.SendToServer(join);                              // 发加入请求
        }

        //---------------注册事件---------------
        private void RegisterHandlers()
        {
            _handlers[MessageType.JoinResponse]       = OnJoinResponse;
            _handlers[MessageType.PlayerJoinedNotice] = OnPlayerJoinedNotice;
            _handlers[MessageType.PlayerLeftNotice]   = OnPlayerLeftNotice;
            _handlers[MessageType.PlayerStateSync]    = OnPlayerStateSync;
            _handlers[MessageType.ChatMessage]        = OnChatMessage;
        }

        //---------------事件函数---------------
        private void OnJoinResponse(IMessage msg)
        {
            var response = (JoinResponse)msg;
            _localPlayerId = response.AssignedPlayerId;                 // 记下我的 ID
            _players.Clear();                                           // 清空旧列表
            foreach (var p in response.Players)
                _players[p.PlayerId] = p.PlayerName;                    // 填房间快照
        }

        private void OnPlayerJoinedNotice(IMessage msg)
        {
            var notice = (PlayerJoinedNotice)msg;
            _players[notice.PlayerId] = notice.PlayerName;              // 新玩家加入列表
        }

        private void OnPlayerLeftNotice(IMessage msg)
        {
            var notice = (PlayerLeftNotice)msg;
            _players.Remove(notice.PlayerId);                           // 玩家离开列表
        }

        private void OnPlayerStateSync(IMessage msg)
        {
            var sync = (PlayerStateSync)msg;
            // Day 4 接渲染时用：更新对应玩家的位置（现在先存/忽略）
        }

        private void OnChatMessage(IMessage msg)
        {
            var chat = (ChatMessage)msg;
            // Day 4 接 UI 时用：显示聊天（现在先忽略）
        }

        // ========== 发送辅助 ==========
        public void SendChat(string text, ulong targetId = 0)
        {
            var msg = new ChatMessage(targetId, text);
            msg.SenderId = _localPlayerId;                              // 自动填我的 ID
            _transport.SendToServer(msg);
        }
    }
}