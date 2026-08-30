using UnityEngine;

namespace Player
{
    public class IdlePlayerState : PlayerState
    {
        protected override void OnEnter(Player entity)
        {
            Debug.Log("IdlePlayerState: entered");
        }

        protected override void OnExit(Player entity)
        {
            Debug.Log("IdlePlayerState: exited");
        }

        protected override void OnStep(Player entity)
        {
            Debug.Log("IdlePlayerState: stepping, timeSinceEntered = " + timeSinceEntered);
            entity.SlowDown();
            if (entity.input.GetMovement().sqrMagnitude > 0 || entity.lateralVelocity.sqrMagnitude > 0)
            {
                entity.states.Change<WalkPlayerState>();
            }
        }
    }
}
