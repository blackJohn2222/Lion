using Core;
using NUnit.Framework;

namespace Test
{
    public class StateMachineTests
    {
        [Test]
        public void InitialState_IsBootstrap()
        {
            var manager = new GameStateManager();

            Assert.AreEqual(GameState.Bootstrap, manager.CurrentState);
        }

        [Test]
        public void ChangeState_ToAllowedState_UpdatesCurrentState()
        {
            var manager = new GameStateManager();

            manager.ChangeState(GameState.MainMenu);

            Assert.AreEqual(GameState.MainMenu, manager.CurrentState);
        }

        [Test]
        public void ChangeState_ToIllegalState_Throws()
        {
            var manager = new GameStateManager();

            Assert.Throws<System.InvalidOperationException>(
                () => manager.ChangeState(GameState.InGame));
        }

        [Test]
        public void ChangeState_RaisesStateChangedEvent()
        {
            var manager = new GameStateManager();
            GameState? received = null;
            manager.StateChanged += (s) => received = s;

            manager.ChangeState(GameState.MainMenu);

            Assert.AreEqual(GameState.MainMenu, received);
        }

        [Test]
        public void Disconnected_IsEndState()
        {
            var manager = new GameStateManager();
            manager.ChangeState(GameState.MainMenu);
            manager.ChangeState(GameState.Connecting);
            manager.ChangeState(GameState.Disconnected);

            Assert.Throws<System.InvalidOperationException>(
                () => manager.ChangeState(GameState.MainMenu));
        }
    }
}
