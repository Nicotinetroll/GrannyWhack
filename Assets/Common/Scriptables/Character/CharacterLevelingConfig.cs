using UnityEngine;

namespace OctoberStudio
{
    [CreateAssetMenu(
        fileName = "CharacterLevelingConfig",
        menuName = "OctoberStudio/Character/Leveling Config")]
    public class CharacterLevelingConfig : ScriptableObject
    {
        [Header("Levels")]
        [SerializeField, Min(1)]
        private int maxLevel = 25;

        [Header("Raw XP per Level (1→max)")]
        [Tooltip("XP needed to go from lvl 1→2")]
        [SerializeField, Min(0)]
        private float firstLevelXP = 1f;

        [Tooltip("XP needed to go from lvl ( max-1)→max")]
        [SerializeField, Min(0)]
        private float lastLevelXP  = 25f;

        [Header("XP Sources")]
        [SerializeField] private float xpPerKill        = 1f;
        [SerializeField] private float xpPerDamage      = 0.01f;
        [Tooltip("XP awarded each time the PLAYER levels up")]
        [SerializeField] private float xpPerPlayerLevel = 1f;

        [Header("Damage Scaling")]
        [SerializeField, Min(0)]
        private float damagePerLevel = 1f;

        public int   MaxLevel            => maxLevel;
        public float XpPerKill           => xpPerKill;
        public float XpPerDamage         => xpPerDamage;
        public float XpPerPlayerLevel    => xpPerPlayerLevel;
        public float DamagePerLevel      => damagePerLevel;

        /// <summary>
        /// Raw XP required to go from (level→level+1).
        /// Linearly ramps firstLevelXP→lastLevelXP over [1..maxLevel].
        /// </summary>
        public float GetRawXpForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, maxLevel);
            if (maxLevel <= 1) return lastLevelXP;
            float t = (level - 1f) / (maxLevel - 1f);
            return Mathf.Lerp(firstLevelXP, lastLevelXP, t);
        }

        /// <summary>
        /// Cumulative XP required to reach exactly <paramref name="level"/>.
        /// (sum of raw XP from lvl 1 up to that level).
        /// </summary>
        public float GetCumulativeXpForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, maxLevel);
            float sum = 0f;
            for (int i = 1; i < level; i++)
                sum += GetRawXpForLevel(i);
            return sum;
        }

        /// <summary>
        /// Legacy API: Cumulative XP required to reach <paramref name="level"/>.
        /// </summary>
        public float GetXpForLevel(int level) => GetCumulativeXpForLevel(level);

        /// <summary>
        /// Raw XP required to go from (level→level+1).
        /// </summary>
        public float GetXpForNextLevel(int level) => GetRawXpForLevel(level);
    }
}
