using System;
using System.Collections.Generic; 

namespace Core
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance)
        {
            _services[typeof(T)] = instance;
        }

        public static T Resolve<T>()
        {
            return (T)_services[typeof(T)];
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}