using Entity;

namespace Player
{
    public class PlayerStats : EntityStats<PlayerStats>
    {
        //----------------移动----------------
        public float acceleration = 12f;
        public float deceleration = 18f;
        public float topSpeed = 6f;
        public float turningDrag = 12f;
    }
}