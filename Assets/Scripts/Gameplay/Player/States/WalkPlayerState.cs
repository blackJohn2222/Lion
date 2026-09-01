using Entity;
using UnityEngine;

namespace Player
{
    public class WalkPlayerState : PlayerState
    {
        protected override void OnEnter(Player entity)
        {
            Debug.Log("WalkPlayerState: entered");
        }

        protected override void OnExit(Player entity)
        {
            Debug.Log("WalkPlayerState: exited");
        }

        protected override void OnStep(Player entity)
        {
            Debug.Log("WalkPlayerState: stepping, timeSinceEntered = " + timeSinceEntered);
            entity.SnapToGround();
            entity.SlowDown();
            entity.Move();
            entity.Jump();
            if (entity.input.GetMovement().sqrMagnitude == 0 && entity.lateralVelocity.sqrMagnitude < 0.01f)
            {
                entity.states.Change<IdlePlayerState>();
            }
            
            if (!entity.isGrounded)
            {
                entity.states.Change<FallPlayerState>();
            }
        }
    }
}