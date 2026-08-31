using System;
using UnityEngine.Events;

namespace Entity
{
    [Serializable]
    public class EntityEvents
    {
        /// <summary>
        /// 当实体落地时触发的事件。  
        /// </summary>
        public UnityEvent OnGroundEnter;

        /// <summary>
        /// 当实体离开地面时触发的事件。  
        /// </summary>
        public UnityEvent OnGroundExit;

    }
}