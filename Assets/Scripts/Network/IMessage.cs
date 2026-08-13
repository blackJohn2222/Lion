using Unity.Collections;

namespace Network
{
    public interface IMessage
    {
        ulong SenderId { get; set; }
        MessageType Type { get; }
        void Write(ref DataStreamWriter writer);
        void Read(ref DataStreamReader reader);
    }
}
