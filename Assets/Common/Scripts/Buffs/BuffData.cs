using UnityEngine;

namespace OctoberStudio.Buffs
{
    [CreateAssetMenu(fileName = "BuffData", menuName = "October/Buffs/Buff")]
    public class BuffData : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private BuffType buffType;
        [SerializeField] private string title;

        [Header("Behavior Settings")]
        [SerializeField] private float duration = 10f;

        [Tooltip("Multiplier or effect strength (e.g. 2x for damage, 0.5x for cooldown)")]
        [SerializeField] private float value = 1f;

        [Header("Runtime Prefab")]
        [SerializeField] private RuntimeBuff runtimePrefab;

        public BuffType BuffType => buffType;
        public string Title => title;
        public float Duration => duration;
        public float Value => value; // 👈 Added accessor
        public RuntimeBuff RuntimePrefab => runtimePrefab;
        
    }
}