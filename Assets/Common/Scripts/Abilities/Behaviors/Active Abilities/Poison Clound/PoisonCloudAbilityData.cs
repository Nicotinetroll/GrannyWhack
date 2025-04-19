using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "PoisonCloudAbilityData",
                     menuName = "October/Abilities/Active/Poison Cloud")]
    public class PoisonCloudAbilityData
        : GenericAbilityData<PoisonCloudAbilityLevel>
    {
        private void Awake()      => type = AbilityType.PoisonCloud;
        private void OnValidate() => type = AbilityType.PoisonCloud;
    }

    [System.Serializable]
    public class PoisonCloudAbilityLevel : AbilityLevel
    {
        [Tooltip("Number of clouds to spawn per use")]
        [SerializeField, Min(1)] private int cloudsCount = 3;
        public int CloudsCount => cloudsCount;

        [Tooltip("Time between each cloud spawn")]
        [SerializeField] private float timeBetweenClouds = 0.5f;
        public float TimeBetweenClouds => timeBetweenClouds;

        [Tooltip("Overall cooldown of the ability")]
        [SerializeField] private float abilityCooldown = 5f;
        public float AbilityCooldown => abilityCooldown;

        [Tooltip("Lifetime of each cloud in seconds")]
        [SerializeField] private float cloudLifetime = 4f;
        public float CloudLifetime => cloudLifetime;

        [Tooltip("Radius in world units of each cloud's effect")]
        [SerializeField] private float radius = 2f;
        public float Radius => radius;

        [Tooltip("Damage per tick = Player.Damage * this multiplier")]
        [SerializeField, Min(0.1f)] private float damage = 1f;
        public float Damage => damage;

        [Tooltip("Ticks per second within the cloud")]
        [SerializeField, Min(0.1f)] private float ticksPerSecond = 1f;
        public float TickInterval => 1f / ticksPerSecond;

        [Header("Slow Settings")]
        [Tooltip("Amount to slow enemy movement (0=no slow, 1=full stop)")]
        [SerializeField, Range(0f, 1f)] private float slowAmount = 0.5f;
        public float SlowAmount => slowAmount;

        [Tooltip("Duration in seconds of the slow effect per tick")]
        [SerializeField, Min(0f)] private float slowDuration = 1f;
        public float SlowDuration => slowDuration;
    }
}
