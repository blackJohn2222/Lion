using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(CharacterController))]
    public abstract class EntityBase : MonoBehaviour
    {
        public EntityEvents entityEvents;
        
        public CharacterController controller { get; protected set; }

        public Vector3 velocity { get; protected set; }

        public Vector3 lateralVelocity
        {
            get => new Vector3(velocity.x, 0f, velocity.z);
            protected set => velocity = new Vector3(value.x, velocity.y, value.z);
        }

        public Vector3 verticalVelocity
        {
            get => new Vector3(0f, velocity.y, 0f);
            protected set => velocity = new Vector3(velocity.x, value.y, velocity.z);
        }

        public bool isGrounded { get; protected set; }
    }
}
