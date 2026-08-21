using System.Collections.Generic;
using Entity;
using UnityEngine;

namespace Gameplay
{
    public class PlayerStateManager : EntityStateManager<Player>
    {
        protected override List<EntityState<Player>> GetStateList()
        {
            return new List<EntityState<Player>>
            {
                new IdlePlayerState()
            };
        }
    }
}
