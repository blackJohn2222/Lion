using System;
using System.Collections.Generic;
using UnityEngine;

namespace Entity
{
    public abstract class EntityStateManagerBase : MonoBehaviour
    {
        public EntityStateManagerEvents events = new();
    }

    public abstract class EntityStateManager<T> : EntityStateManagerBase where T : Entity<T>
    {
        private readonly List<EntityState<T>> _stateList = new List<EntityState<T>>();
        private readonly Dictionary<Type, EntityState<T>> _statesByType = new Dictionary<Type, EntityState<T>>();

        public EntityState<T> current { get; protected set; }
        public EntityState<T> last { get; protected set; }
        public T entity { get; protected set; }

        protected virtual void Start()
        {
            InitializeEntity();
            InitializeStates();
        }

        protected abstract List<EntityState<T>> GetStateList();

        protected virtual void InitializeEntity()
        {
            entity = GetComponent<T>();
            if (entity == null)
            {
                throw new MissingComponentException(
                    $"{GetType().Name} requires an entity component of type {typeof(T).Name}.");
            }
        }

        protected virtual void InitializeStates()
        {
            var registeredStates = GetStateList();
            if (registeredStates == null || registeredStates.Count == 0)
            {
                throw new InvalidOperationException($"{GetType().Name} must register at least one state.");
            }

            _stateList.Clear();
            _statesByType.Clear();

            foreach (var state in registeredStates)
            {
                if (state == null)
                {
                    throw new InvalidOperationException($"{GetType().Name} cannot register a null state.");
                }

                var stateType = state.GetType();
                if (_statesByType.ContainsKey(stateType))
                {
                    throw new InvalidOperationException(
                        $"{GetType().Name} registered the state type {stateType.Name} more than once.");
                }

                _stateList.Add(state);
                _statesByType.Add(stateType, state);
            }

            Change(_stateList[0]);
        }

        public virtual void Change<TState>() where TState : EntityState<T>
        {
            if (!_statesByType.TryGetValue(typeof(TState), out var nextState))
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} does not contain a state of type {typeof(TState).Name}.");
            }

            Change(nextState);
        }

        public virtual void Change(EntityState<T> nextState)
        {
            if (nextState == null)
            {
                throw new ArgumentNullException(nameof(nextState));
            }

            if (ReferenceEquals(current, nextState))
            {
                return;
            }

            if (current != null)
            {
                current.Exit(entity);
                events.onExit?.Invoke(current.GetType());
                last = current;
            }

            current = nextState;
            current.Enter(entity);
            events.onEnter?.Invoke(current.GetType());
            events.onChange?.Invoke();
        }

        public virtual void Step()
        {
            if (current == null || entity == null)
            {
                return;
            }

            current.Step(entity);
        }

        public virtual void OnContact(Collider other)
        {
            if (current == null || entity == null)
            {
                return;
            }

            current.OnContact(entity, other);
        }
    }
}
