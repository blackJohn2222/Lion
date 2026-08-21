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
    }
}
