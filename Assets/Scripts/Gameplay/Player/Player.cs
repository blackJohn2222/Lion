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
            Accelerate(direction, stats.current.acceleration, stats.current.topSpeed, stats.current.turningDrag);
        }

        public void SlowDown()
        {
            Decelerate(stats.current.deceleration);
        }
    }
}
