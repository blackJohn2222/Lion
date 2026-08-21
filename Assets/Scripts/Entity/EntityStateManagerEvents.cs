using System;
using UnityEngine.Events;

namespace Entity
{
    [Serializable]
    public class EntityStateManagerEvents
    {
        public UnityEvent onChange = new();
        public UnityEvent<Type> onEnter = new();
        public UnityEvent<Type> onExit = new();
    }
}
