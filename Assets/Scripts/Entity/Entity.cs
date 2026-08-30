using UnityEngine;

namespace Entity
{
    public abstract class Entity<T> : EntityBase where T : Entity<T>
    {
        public EntityStateManager<T> states { get; protected set; }

        protected override void Awake()
        {
            base.Awake();

            states = GetComponent<EntityStateManager<T>>();
            if (states == null)
            {
                throw new MissingComponentException(
                    $"{GetType().Name} requires an EntityStateManager<{typeof(T).Name}> component.");
            }
        }

        protected override void Update()
        {
            states?.Step();
            base.Update();
        }
        
        public void Accelerate(Vector3 direction, float acceleration, float topSpeed, float turningDrag)
        {
            var projected = Vector3.Project(lateralVelocity, direction.normalized);
            lateralVelocity = Vector3.Lerp(lateralVelocity, projected, turningDrag * Time.deltaTime);
            
            lateralVelocity += direction.normalized * (acceleration * Time.deltaTime);
            
            if (lateralVelocity.magnitude > topSpeed)
            {
                lateralVelocity = lateralVelocity.normalized * topSpeed;
            }
        }

        public void Decelerate(float deceleration)
        {
            lateralVelocity = lateralVelocity.normalized * Mathf.Max(0f, lateralVelocity.magnitude - deceleration * Time.deltaTime);
        }
    }
}
