using System;

namespace Core
{
    public class GameStateManager
    {
        private readonly StateMachine<GameState> _machine = new();
        
        public GameState CurrentState => _machine.CurrentState;
        
        public event Action<GameState> StateChanged;
        
        public GameStateManager()
        {
            _machine.StateChanged += (s) => StateChanged?.Invoke(s);
            
            _machine.Configure(GameState.Bootstrap,    GameState.MainMenu);
            _machine.Configure(GameState.MainMenu,     GameState.Connecting);
            _machine.Configure(GameState.Connecting,   GameState.InGame, GameState.Disconnected);
            _machine.Configure(GameState.InGame,       GameState.Paused, GameState.Disconnected);
            _machine.Configure(GameState.Paused,       GameState.InGame, GameState.Disconnected);
            _machine.Configure(GameState.Disconnected);

            _machine.SetInitialState(GameState.Bootstrap);
        }
        
        public void ChangeState(GameState next) => _machine.ChangeState(next);
    }
}