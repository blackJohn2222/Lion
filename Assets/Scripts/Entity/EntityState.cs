using UnityEngine;
using UnityEngine.Events;

namespace Entity
{
    public abstract class EntityState<T> where T : Entity<T>
    {
        public UnityEvent onEnter = new UnityEvent();
        public UnityEvent onExit = new UnityEvent();

        public float timeSinceEntered { get; protected set; }

        public void Enter(T entity)
        {
            timeSinceEntered = 0f;
            onEnter?.Invoke();
            OnEnter(entity);
        }

        public void Exit(T entity)
        {
            onExit?.Invoke();
            OnExit(entity);
        }

        public void Step(T entity)
        {
            OnStep(entity);
            timeSinceEntered += Time.deltaTime;
        }

        protected abstract void OnEnter(T entity);

        protected abstract void OnExit(T entity);

        protected abstract void OnStep(T entity);

        public virtual void OnContact(T entity, Collider other)
        {
        }
    }
}
