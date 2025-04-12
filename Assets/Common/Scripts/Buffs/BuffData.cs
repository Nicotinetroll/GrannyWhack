using UnityEngine;

namespace OctoberStudio.Buffs
{
    [CreateAssetMenu(fileName = "BuffData", menuName = "October/Buffs/Buff")]
    public class BuffData : ScriptableObject
    {
        [SerializeField] private BuffType buffType;
        [SerializeField] private float duration = 10f;
        [SerializeField] private RuntimeBuff runtimePrefab;

        public BuffType BuffType => buffType;
        public float Duration => duration;
        public RuntimeBuff RuntimePrefab => runtimePrefab;
    }
}