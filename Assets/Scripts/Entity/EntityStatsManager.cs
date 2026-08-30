using UnityEngine;
using UnityEngine.Serialization;

namespace Entity
{
    public abstract class EntityStatsManager<T> : MonoBehaviour where T : EntityStats<T>
    {
        public T[] stats;
        
        public T current { get; protected set; }

        protected virtual void Start()
        {
            if (stats != null && stats.Length > 0)
            {
                current = stats[0];
            }
        }

        public virtual void Change(int to)
        {
            if(stats.Length > to && to >= 0)
            {
                // 如果切换的不是当前属性，则进行切换
                if (current != stats[to])
                {
                    current = stats[to];
                }
            }
        }
    }
}