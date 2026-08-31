using Entity;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(PlayerInputManager))]
    [RequireComponent(typeof(PlayerStateManager))]
    [RequireComponent(typeof(PlayerStatsManager))]
    public class Player : Entity<Player>
    {
        public PlayerInputManager input { get; protected set; }
        public PlayerStatsManager stats { get; protected set; }

        protected override void Awake()
        {
            base.Awake();
            input = GetComponent<PlayerInputManager>();
            stats = GetComponent<PlayerStatsManager>();
        }

        public void Move()
        {
            var movement = input.GetMovement();
            var direction = new Vector3(movement.x, 0, movement.y);
            Accelerate(direction, stats.current.maxAcceleration, stats.current.topSpeed);
        }

        public void SlowDown()
        {
            Decelerate(stats.current.friction, stats.current.stopSpeed);
        }
        
        public void Gravity() => base.Gravity(stats.current.gravity);

        public void SnapToGround() => base.SnapToGround(stats.current.snapForce);
    }
}
