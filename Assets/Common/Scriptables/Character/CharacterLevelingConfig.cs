using UnityEngine;

namespace OctoberStudio
{
    /// <summary>Designer‑tweakable numbers for character leveling.</summary>
    [CreateAssetMenu(
        fileName = "CharacterLevelingConfig",
        menuName = "OctoberStudio/Character/Leveling Config")]
    public class CharacterLevelingConfig : ScriptableObject
    {
        [Header("Leveling")]
        [SerializeField, Min(1)]
        int maxLevel = 50;

        [Tooltip("Curve mapping level → XP required to reach that level (not cumulative).")]
        [SerializeField]
        AnimationCurve xpCurve = AnimationCurve.Linear(1, 0, 50, 1000);

        [Tooltip("XP awarded per enemy kill.")]
        [SerializeField]
        float xpPerKill = 1f;

        [Header("Damage Scaling")]
        [Tooltip("Flat damage bonus per level (level 1 = +0).")]
        [SerializeField, Min(0)]
        float damagePerLevel = 1f;

        /// <summary>Maximum achievable level.</summary>
        public int MaxLevel => maxLevel;

        /// <summary>XP awarded for each kill.</summary>
        public float XpPerKill => xpPerKill;

        /// <summary>Flat damage bonus gained per level beyond level 1.</summary>
        public float DamagePerLevel => damagePerLevel;

        /// <summary>
        /// XP needed **to reach** the given <paramref name="level"/> (not cumulative).
        /// </summary>
        public float GetXpForLevel(int level) => xpCurve.Evaluate(level);
    }
}