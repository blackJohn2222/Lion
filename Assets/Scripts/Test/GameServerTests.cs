using NUnit.Framework;

namespace Network
{
    public class GameServerTests
    {
        private FakeTransport _fake;
        private GameServer _server;

        private void SetupServer()
        {
            _fake = new FakeTransport();
            _server = new GameServer(_fake);
            _server.Start(7777);
        }

        // 模拟客户端连上服务器 + 发加入请求
        private void ClientJoins(ulong clientId, string name)
        {
            _fake.SimulateClientConnected(clientId);      // 传输层分配 ID（OnClientConnected）
            _fake.SimulateOnMessageReceived(clientId, new JoinRequest(name));  // 收到加入请求
        }

        [Test]
        public void JoinRequest_SendsJoinResponse_WithPlayerId()
        {
            SetupServer();
            ClientJoins(1, "Leo");

            var response = _fake.FindSent<JoinResponse>();
            Assert.IsNotNull(response, "应发出 JoinResponse");
            Assert.AreEqual(1UL, response.AssignedPlayerId);
        }

        [Test]
        public void JoinRequest_BroadcastsPlayerJoinedNotice()
        {
            SetupServer();
            ClientJoins(1, "Leo");

            var notice = _fake.FindSent<PlayerJoinedNotice>();
            Assert.IsNotNull(notice, "应广播 PlayerJoinedNotice");
            Assert.AreEqual(1UL, notice.PlayerId);
            Assert.AreEqual("Leo", notice.PlayerName);
        }

        [Test]
        public void JoinResponse_ContainsExistingPlayers()
        {
            SetupServer();
            ClientJoins(1, "Leo");   // 先加入 1 号
            _fake.Sent.Clear();      // 清掉 1 号加入时的消息
            ClientJoins(2, "Kai");   // 2 号加入

            var response = _fake.FindSent<JoinResponse>();
            Assert.AreEqual(2UL, response.AssignedPlayerId);
            // 2 号的 JoinResponse 应含 1 号 Leo
            Assert.AreEqual(1, response.Players.Count);
            Assert.AreEqual("Leo", response.Players[0].PlayerName);
        }

        [Test]
        public void ChatMessage_Target0_Broadcasts()
        {
            SetupServer();
            ClientJoins(1, "Leo");
            _fake.Sent.Clear();

            var chat = new ChatMessage(0, "hello");
            _fake.SimulateOnMessageReceived(1, chat);

            var sent = _fake.FindSent<ChatMessage>();
            Assert.IsNotNull(sent, "应广播聊天");
            Assert.AreEqual("hello", sent.Text);
        }

        [Test]
        public void ClientDisconnect_RemovesPlayer_AndBroadcastsLeft()
        {
            SetupServer();
            ClientJoins(1, "Leo");
            _fake.Sent.Clear();

            _fake.SimulateClientDisconnected(1);

            var notice = _fake.FindSent<PlayerLeftNotice>();
            Assert.IsNotNull(notice, "应广播 PlayerLeftNotice");
            Assert.AreEqual(1UL, notice.PlayerId);
        }

        [Test]
        public void ClientDisconnect_BroadcastLeft_OnlyIfPlayerExisted()
        {
            SetupServer();
            // 没有玩家加入，直接断开一个不存在的 ID
            _fake.SimulateClientDisconnected(99);

            var notice = _fake.FindSent<PlayerLeftNotice>();
            Assert.IsNull(notice, "不该为不存在的玩家广播 PlayerLeftNotice");
        }
    }
}
