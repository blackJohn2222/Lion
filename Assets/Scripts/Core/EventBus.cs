using System;
using System.Collections.Generic;

namespace Core
{
    public static class EventBus<T>
    {
        private static List<Action<T>> _subscribers = new();

        public static void Subscribe(Action<T> action)
        {
            _subscribers.Add(action);
        }

        public static void Unsubscribe(Action<T> action)
        {
            _subscribers.Remove(action);
        }

        public static void Publish(T e)
        {
            var snapshot = new List<Action<T>>(_subscribers);
            foreach (var h in snapshot)
            {
                h(e);
            }
        }
    }
}