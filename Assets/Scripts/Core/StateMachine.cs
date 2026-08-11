using System;
using System.Collections.Generic;

namespace Core
{
    public class StateMachine<T>
    {
        private readonly Dictionary<T, HashSet<T>> _allowedTransitions = new();
        
        public T CurrentState { get; private set; }
        
        public event Action<T> StateChanged;

        public void Configure(T from, params T[] toStates)
        {
            _allowedTransitions[from] = new HashSet<T>(toStates);
        }

        public bool TryChangeState(T newState)
        {
            if (!_allowedTransitions[CurrentState].Contains(newState))
            {
                return false;
            }
            
            CurrentState = newState;
            StateChanged?.Invoke(CurrentState);
            return true;
        }

        public void ChangeState(T newState)
        {
            if (!TryChangeState(newState))
            {
                throw new InvalidOperationException($"非法转移: {CurrentState} → {newState}");
            }
        }
        
        public void SetInitialState(T initialState)
        {
            CurrentState = initialState;
            StateChanged?.Invoke(CurrentState);
        }
    }
}