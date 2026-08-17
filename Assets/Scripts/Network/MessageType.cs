namespace Network
{
    // ID 分段：1xxx = 客户端→服务器指令，2xxx = 服务器→客户端通知
    public enum MessageType : ushort
    {
        // --- 客户端 → 服务器 ---
        JoinRequest = 1001,
        PlayerStateSync = 1002,
        ChatMessage = 1003,         // 双方都有
        DisconnectNotice = 1004,

        // --- 服务器 → 客户端 ---
        PlayerJoinedNotice = 2001,
        PlayerLeftNotice = 2002,
        SnapshotMessage = 2003,
        JoinResponse = 2004
    }
}
