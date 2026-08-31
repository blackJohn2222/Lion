using Entity;
using UnityEngine;

namespace Player
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "ScriptableObjects/PlayerStats")]
    public class PlayerStats : EntityStats<PlayerStats>
    {
        //----------------移动----------------
        [Header("Move")]
        public float maxAcceleration = 5.5f;
        public float topSpeed = 6f;
        public float friction = 4f;
        public float stopSpeed = 0.5f;
        
        //----------------下落----------------
        [Header("Fall")]
        public float gravity = 20f;
        public float snapForce = 3f;
    }
}