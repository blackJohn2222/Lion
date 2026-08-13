using System.Collections.Generic;
using Network;
using NUnit.Framework;

namespace Test
{
    public class SerializeToolTests
    {
        [Test]
        public void JoinRequest_RoundTrips()
        {
            var original = new JoinRequest("Leo");

            var bytes = SerializeTool.Serialize(original);
            var result = (JoinRequest)SerializeTool.Deserialize(bytes);

            Assert.AreEqual("Leo", result.PlayerName);
        }

        [Test]
        public void JoinResponse_RoundTrips_WithPlayerList()
        {
            var players = new List<PlayerInfo>
            {
                new PlayerInfo { PlayerId = 1, PlayerName = "Leo" },
                new PlayerInfo { PlayerId = 2, PlayerName = "Kai" },
            };
            var original = new JoinResponse(42, players);

            var bytes = SerializeTool.Serialize(original);
            var result = (JoinResponse)SerializeTool.Deserialize(bytes);

            Assert.AreEqual(42UL, result.AssignedPlayerId);
            Assert.AreEqual(2, result.Players.Count);
            Assert.AreEqual(1UL, result.Players[0].PlayerId);
            Assert.AreEqual("Leo", result.Players[0].PlayerName);
            Assert.AreEqual(2UL, result.Players[1].PlayerId);
            Assert.AreEqual("Kai", result.Players[1].PlayerName);
        }

        [Test]
        public void PlayerStateSync_RoundTrips()
        {
            var original = new PlayerStateSync(7, 1.5f, -2.5f, 3.0f);

            var bytes = SerializeTool.Serialize(original);
            var result = (PlayerStateSync)SerializeTool.Deserialize(bytes);

            Assert.AreEqual(7UL, result.PlayerId);
            Assert.AreEqual(1.5f, result.PosX);
            Assert.AreEqual(-2.5f, result.PosY);
            Assert.AreEqual(3.0f, result.PosZ);
        }

        [Test]
        public void ChatMessage_RoundTrips()
        {
            var original = new ChatMessage(0, "hello");

            var bytes = SerializeTool.Serialize(original);
            var result = (ChatMessage)SerializeTool.Deserialize(bytes);

            Assert.AreEqual(0UL, result.TargetId);
            Assert.AreEqual("hello", result.Text);
        }

        [Test]
        public void DisconnectNotice_RoundTrips()
        {
            var original = new DisconnectNotice("server closed");

            var bytes = SerializeTool.Serialize(original);
            var result = (DisconnectNotice)SerializeTool.Deserialize(bytes);

            Assert.AreEqual("server closed", result.Reason);
        }

        [Test]
        public void PlayerJoinedNotice_RoundTrips()
        {
            var original = new PlayerJoinedNotice(3, "Mia");

            var bytes = SerializeTool.Serialize(original);
            var result = (PlayerJoinedNotice)SerializeTool.Deserialize(bytes);

            Assert.AreEqual(3UL, result.PlayerId);
            Assert.AreEqual("Mia", result.PlayerName);
        }

        [Test]
        public void PlayerLeftNotice_RoundTrips()
        {
            var original = new PlayerLeftNotice(3);

            var bytes = SerializeTool.Serialize(original);
            var result = (PlayerLeftNotice)SerializeTool.Deserialize(bytes);

            Assert.AreEqual(3UL, result.PlayerId);
        }

        [Test]
        public void SnapshotMessage_RoundTrips_WithPlayerList()
        {
            var players = new List<PlayerInfo>
            {
                new PlayerInfo { PlayerId = 1, PlayerName = "Leo" },
            };
            var original = new SnapshotMessage(players);

            var bytes = SerializeTool.Serialize(original);
            var result = (SnapshotMessage)SerializeTool.Deserialize(bytes);

            Assert.AreEqual(1, result.Players.Count);
            Assert.AreEqual(1UL, result.Players[0].PlayerId);
            Assert.AreEqual("Leo", result.Players[0].PlayerName);
        }

        [Test]
        public void Deserialize_UnknownType_Throws()
        {
            var writer = new Unity.Collections.DataStreamWriter(8, Unity.Collections.Allocator.Temp);
            writer.WriteUShort(9999);
            var bytes = writer.AsNativeArray().ToArray();

            Assert.Throws<System.InvalidOperationException>(() => SerializeTool.Deserialize(bytes));
        }
    }
}
