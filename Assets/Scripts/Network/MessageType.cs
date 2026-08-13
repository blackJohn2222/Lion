namespace Network
{
    // ID 分段：1xxx = 客户端→服务器指令，2xxx = 服务器→客户端通知
    public enum MessageType : ushort
    {
        // --- 客户端 → 服务器 ---
        JoinRequest = 1001,
        JoinResponse = 1002,
        PlayerStateSync = 1003,
        ChatMessage = 1004,
        DisconnectNotice = 1005,

        // --- 服务器 → 客户端 ---
        PlayerJoinedNotice = 2001,
        PlayerLeftNotice = 2002,
        SnapshotMessage = 2003
    }
}
