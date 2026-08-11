namespace Core
{
    public enum GameState
    {
        Bootstrap,      // 启动：程序出生点
        MainMenu,       // 主菜单
        Connecting,     // 连接中
        InGame,         // 游戏内
        Paused,         // 暂停
        Disconnected    // 断线
    }
}