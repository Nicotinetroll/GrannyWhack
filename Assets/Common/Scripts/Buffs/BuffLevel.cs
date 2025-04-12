using UnityEngine;

namespace OctoberStudio.Buffs
{
    [System.Serializable]
    public class BuffLevel
    {
        [SerializeField] private float duration = 5f;
        [SerializeField] private float value = 2f;

        public float Duration => duration;
        public float Value => value;
    }
}