using UnityEngine;

namespace OctoberStudio
{
    [CreateAssetMenu(
        fileName = "CharacterLevelingConfig",
        menuName = "OctoberStudio/Character/Leveling Config")]
    public class CharacterLevelingConfig : ScriptableObject
    {
        [Header("Leveling Curve")]
        [SerializeField, Min(1)]    int maxLevel        = 25;
        [SerializeField]            AnimationCurve xpCurve = AnimationCurve.Linear(1, 0, 25, 100);
        
        [Header("XP Sources")]
        [Tooltip("XP awarded per enemy kill (if you still want this)")]
        [SerializeField]            float xpPerKill     = 1f;
        [Tooltip("XP awarded per damage point dealt (if you still want this)")]
        [SerializeField]            float xpPerDamage   = 0.01f;
        [Tooltip("XP awarded each completed stage")]
        [SerializeField]            float xpPerStage    = 1f;

        [Header("Damage Scaling")]
        [Tooltip("Flat damage bonus per level (level 1 = +0)")]
        [SerializeField, Min(0)]    float damagePerLevel = 1f;

        public int   MaxLevel       => maxLevel;
        public float XpPerKill      => xpPerKill;
        public float XpPerDamage    => xpPerDamage;
        public float XpPerStage     => xpPerStage;
        public float DamagePerLevel => damagePerLevel;

        /// <summary>XP needed to reach exactly the given level (not cumulative).</summary>
        public float GetXpForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, maxLevel);
            return xpCurve.Evaluate(level);
        }
    }
}