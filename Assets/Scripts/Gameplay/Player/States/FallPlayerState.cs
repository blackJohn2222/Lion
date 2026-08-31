using UnityEngine;

namespace Player
{
    public class FallPlayerState : PlayerState
    {
        protected override void OnEnter(Player entity)
        {
            Debug.Log("FallPlayerState: entered");
        }

        protected override void OnExit(Player entity)
        {
            Debug.Log("FallPlayerState: exited");
        }

        protected override void OnStep(Player entity)
        {
            Debug.Log("FallPlayerState: stepping, timeSinceEntered = " + timeSinceEntered);
            entity.Gravity();
            entity.Move();

            if (entity.isGrounded)
            {
                entity.states.Change<IdlePlayerState>();
            }
        }
    }
}