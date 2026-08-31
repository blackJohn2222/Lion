using System.Collections.Generic;
using Entity;


namespace Player
{
    public class PlayerStateManager : EntityStateManager<Player>
    {
        protected override List<EntityState<Player>> GetStateList()
        {
            return new List<EntityState<Player>>
            {
                new IdlePlayerState(),
                new WalkPlayerState(),
                new FallPlayerState()
            };
        }
    }
}
