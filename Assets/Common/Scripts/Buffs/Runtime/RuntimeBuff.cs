using UnityEngine;

namespace OctoberStudio.Buffs
{
    public abstract class RuntimeBuff : MonoBehaviour
    {
        protected BuffData data;

        public void Init(BuffData buffData)
        {
            data = buffData;
            Apply();
            Invoke(nameof(Remove), data.Duration);
        }

        protected abstract void Apply();
        protected abstract void Remove();
    }
}