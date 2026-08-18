using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;

namespace Network
{
    public static class SerializeTool
    {
        private readonly static Dictionary<MessageType, Func<IMessage>> _factories = new()
        {
            [MessageType.JoinResponse] = () => new JoinResponse(),
            [MessageType.JoinRequest] = () => new JoinRequest(),
            [MessageType.PlayerStateSync] = () => new PlayerStateSync(),
            [MessageType.ChatMessage] = () => new ChatMessage(),
            [MessageType.DisconnectNotice] = () => new DisconnectNotice(),
            [MessageType.PlayerJoinedNotice] = () => new PlayerJoinedNotice(),
            [MessageType.PlayerLeftNotice] = () => new PlayerLeftNotice(),
            [MessageType.SnapshotMessage] = () => new SnapshotMessage(),
        };

        public static byte[] Serialize(IMessage msg)
        {
            var writer = new DataStreamWriter(64, Allocator.Temp);
            
            writer.WriteUShort((ushort)msg.Type);
            msg.Write(ref writer);
            
            return writer.AsNativeArray().ToArray();
        }

        public static IMessage Deserialize(byte[] data)
        {
            var nativeArray = new NativeArray<byte>(data, Allocator.Temp);
            var reader = new DataStreamReader(nativeArray);
            
            var id = (MessageType)reader.ReadUShort();
            if (!_factories.TryGetValue(id, out var factory))
            {
                throw new InvalidOperationException($"未知消息类型: {id}");
            }
            var msg = factory();
            msg.Read(ref reader);

            nativeArray.Dispose();
            return msg;
        }
    }
}