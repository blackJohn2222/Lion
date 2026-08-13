using System.Collections.Generic;
using Unity.Collections;

namespace Network
{
    public class JoinRequest : IMessage
    {
        public ulong SenderId { get; set; }
        public MessageType Type => MessageType.JoinRequest;
        public string PlayerName;

        public JoinRequest() { }
        public JoinRequest(string playerName)
        {
            this.PlayerName = playerName;
        }

        public void Write(ref DataStreamWriter writer)
        {
            writer.WriteFixedString64(PlayerName);
        }

        public void Read(ref DataStreamReader reader)
        {
            PlayerName = reader.ReadFixedString64().ToString();
        }
    }

    public class JoinResponse : IMessage
    {
        public ulong SenderId { get; set; }
        public MessageType Type => MessageType.JoinResponse;
        public ulong AssignedPlayerId;
        public List<PlayerInfo> Players;

        public JoinResponse() { }
        public JoinResponse(ulong assignedPlayerId, List<PlayerInfo> players)
        {
            this.AssignedPlayerId = assignedPlayerId;
            this.Players = players;
        }

        public void Write(ref DataStreamWriter writer)
        {
            writer.WriteULong(AssignedPlayerId);
            writer.WriteInt(Players.Count);
            foreach (var p in Players)
            {
                writer.WriteULong(p.PlayerId);
                writer.WriteFixedString64(p.PlayerName);
            }
        }

        public void Read(ref DataStreamReader reader)
        {
            AssignedPlayerId = reader.ReadULong();
            int count = reader.ReadInt();
            Players = new List<PlayerInfo>(count);
            for (int i = 0; i < count; i++)
            {
                var p = new PlayerInfo();
                p.PlayerId = reader.ReadULong();
                p.PlayerName = reader.ReadFixedString64().ToString();
                Players.Add(p);
            }
        }
    }

    public class PlayerStateSync : IMessage
    {
        public ulong SenderId { get; set; }
        public MessageType Type => MessageType.PlayerStateSync;
        public ulong PlayerId;
        public float PosX, PosY, PosZ;

        public PlayerStateSync() { }
        public PlayerStateSync(ulong playerId, float posX, float posY, float posZ)
        {
            this.PlayerId = playerId;
            this.PosX = posX;
            this.PosY = posY;
            this.PosZ = posZ;
        }

        public void Write(ref DataStreamWriter writer)
        {
            writer.WriteULong(PlayerId);
            writer.WriteFloat(PosX);
            writer.WriteFloat(PosY);
            writer.WriteFloat(PosZ);
        }

        public void Read(ref DataStreamReader reader)
        {
            PlayerId = reader.ReadULong();
            PosX = reader.ReadFloat();
            PosY = reader.ReadFloat();
            PosZ = reader.ReadFloat();
        }
    }

    public class ChatMessage : IMessage
    {
        public ulong SenderId { get; set; }
        public MessageType Type => MessageType.ChatMessage;
        public ulong TargetId;
        public string Text;

        public ChatMessage() { }
        public ChatMessage(ulong targetId, string text)
        {
            this.TargetId = targetId;
            this.Text = text;
        }

        public void Write(ref DataStreamWriter writer)
        {
            writer.WriteULong(TargetId);
            writer.WriteFixedString64(Text);
        }

        public void Read(ref DataStreamReader reader)
        {
            TargetId = reader.ReadULong();
            Text = reader.ReadFixedString64().ToString();
        }
    }

    public class DisconnectNotice : IMessage
    {
        public ulong SenderId { get; set; }
        public MessageType Type => MessageType.DisconnectNotice;
        public string Reason;

        public DisconnectNotice() { }
        public DisconnectNotice(string reason)
        {
            this.Reason = reason;
        }

        public void Write(ref DataStreamWriter writer)
        {
            writer.WriteFixedString64(Reason);
        }

        public void Read(ref DataStreamReader reader)
        {
            Reason = reader.ReadFixedString64().ToString();
        }
    }

    public class PlayerJoinedNotice : IMessage
    {
        public ulong SenderId { get; set; }
        public MessageType Type => MessageType.PlayerJoinedNotice;
        public ulong PlayerId;
        public string PlayerName;

        public PlayerJoinedNotice() { }
        public PlayerJoinedNotice(ulong playerId, string playerName)
        {
            this.PlayerId = playerId;
            this.PlayerName = playerName;
        }

        public void Write(ref DataStreamWriter writer)
        {
            writer.WriteULong(PlayerId);
            writer.WriteFixedString64(PlayerName);
        }

        public void Read(ref DataStreamReader reader)
        {
            PlayerId = reader.ReadULong();
            PlayerName = reader.ReadFixedString64().ToString();
        }
    }

    public class PlayerLeftNotice : IMessage
    {
        public ulong SenderId { get; set; }
        public MessageType Type => MessageType.PlayerLeftNotice;
        public ulong PlayerId;

        public PlayerLeftNotice() { }
        public PlayerLeftNotice(ulong playerId)
        {
            this.PlayerId = playerId;
        }

        public void Write(ref DataStreamWriter writer)
        {
            writer.WriteULong(PlayerId);
        }

        public void Read(ref DataStreamReader reader)
        {
            PlayerId = reader.ReadULong();
        }
    }

    public class SnapshotMessage : IMessage
    {
        public ulong SenderId { get; set; }
        public MessageType Type => MessageType.SnapshotMessage;
        public List<PlayerInfo> Players;

        public SnapshotMessage() { }
        public SnapshotMessage(List<PlayerInfo> players)
        {
            this.Players = players;
        }

        public void Write(ref DataStreamWriter writer)
        {
            writer.WriteInt(Players.Count);
            foreach (var p in Players)
            {
                writer.WriteULong(p.PlayerId);
                writer.WriteFixedString64(p.PlayerName);
            }
        }

        public void Read(ref DataStreamReader reader)
        {
            int count = reader.ReadInt();
            Players = new List<PlayerInfo>(count);
            for (int i = 0; i < count; i++)
            {
                var p = new PlayerInfo();
                p.PlayerId = reader.ReadULong();
                p.PlayerName = reader.ReadFixedString64().ToString();
                Players.Add(p);
            }
        }
    }
}
