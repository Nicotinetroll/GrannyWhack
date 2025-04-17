// CharacterLevelingConfig.cs
using UnityEngine;

namespace OctoberStudio
{
    /// <summary>Designer‑tweakable numbers, no magic constants hidden in code.</summary>
    [CreateAssetMenu(
        fileName = "CharacterLevelingConfig",
        menuName = "OctoberStudio/Character/Leveling Config")]
    public class CharacterLevelingConfig : ScriptableObject
    {
        [SerializeField, Min(1)]  int  maxLevel      = 50;
        [SerializeField] AnimationCurve xpCurve      = AnimationCurve.Linear(1, 0, 50, 1_000);
        [SerializeField] float xpPerKill             = 1f;
        [SerializeField] float xpPerDamage           = 0.01f;

        public int   MaxLevel           => maxLevel;
        public float XpPerKill          => xpPerKill;
        public float XpPerDamage        => xpPerDamage;

        /// <returns>XP needed **to reach** <paramref name="level"/> (not cumulative).</returns>
        public float GetXpForLevel(int level) => xpCurve.Evaluate(level);
    }
}