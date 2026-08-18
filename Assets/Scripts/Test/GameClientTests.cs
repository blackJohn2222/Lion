using NUnit.Framework;

namespace Network
{
    public class GameClientTests
    {
        private FakeTransport _fake;
        private GameClient _client;

        private void SetupClient()
        {
            _fake = new FakeTransport();
            _client = new GameClient(_fake, "Leo");
            _client.Connect("127.0.0.1", 7777);
        }

        // 模拟服务器发消息给客户端
        private void ServerSends(IMessage msg)
        {
            _fake.SimulateOnMessageReceived(0, msg);
        }

        [Test]
        public void Connect_SendsJoinRequest()
        {
            SetupClient();

            _fake.SimulateOnConnected();          // 连上 → 应发 JoinRequest

            var join = _fake.FindSent<JoinRequest>();
            Assert.IsNotNull(join, "连接后应发 JoinRequest");
            Assert.AreEqual("Leo", join.PlayerName);
        }

        [Test]
        public void JoinResponse_SetsLocalPlayerId()
        {
            SetupClient();

            ServerSends(new JoinResponse(3, new System.Collections.Generic.List<PlayerInfo>()));

            Assert.AreEqual(3UL, _client.LocalPlayerId);
        }

        [Test]
        public void JoinResponse_FillsPlayerList()
        {
            SetupClient();

            var players = new System.Collections.Generic.List<PlayerInfo>
            {
                new PlayerInfo { PlayerId = 1, PlayerName = "Leo" },
                new PlayerInfo { PlayerId = 2, PlayerName = "Kai" },
            };
            ServerSends(new JoinResponse(3, players));

            Assert.AreEqual(2, _client.Players.Count);
            Assert.AreEqual("Leo", _client.Players[1]);
            Assert.AreEqual("Kai", _client.Players[2]);
        }

        [Test]
        public void PlayerJoinedNotice_AddsToPlayerList()
        {
            SetupClient();
            ServerSends(new JoinResponse(3, new System.Collections.Generic.List<PlayerInfo>()));  // 先加入

            ServerSends(new PlayerJoinedNotice(2, "Kai"));   // 新人加入

            Assert.IsTrue(_client.Players.ContainsKey(2));
            Assert.AreEqual("Kai", _client.Players[2]);
        }

        [Test]
        public void PlayerLeftNotice_RemovesFromPlayerList()
        {
            SetupClient();
            var players = new System.Collections.Generic.List<PlayerInfo>
            {
                new PlayerInfo { PlayerId = 1, PlayerName = "Leo" },
                new PlayerInfo { PlayerId = 2, PlayerName = "Kai" },
            };
            ServerSends(new JoinResponse(3, players));   // 房间有 1、2 号

            ServerSends(new PlayerLeftNotice(2));        // 2 号离开

            Assert.IsFalse(_client.Players.ContainsKey(2));
            Assert.IsTrue(_client.Players.ContainsKey(1));
        }
    }
}
