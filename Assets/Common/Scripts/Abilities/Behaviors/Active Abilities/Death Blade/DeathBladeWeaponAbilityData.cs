// DeathBladeWeaponAbilityData.cs
using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "DeathBladeWeaponAbilityData", menuName = "October/Abilities/Active/Death Blade")]
    public class DeathBladeWeaponAbilityData : GenericAbilityData<DeathBladeWeaponAbilityLevel>
    {
        private void Awake()    => type = AbilityType.DeathBlade;
        private void OnValidate() => type = AbilityType.DeathBlade;
    }

    [System.Serializable]
    public class DeathBladeWeaponAbilityLevel : AbilityLevel
    {
        [Tooltip("Time between volleys")]
        [SerializeField] private float abilityCooldown;
        public float AbilityCooldown => abilityCooldown;

        [Tooltip("Number of slashes per volley")]
        [SerializeField] private int slashesCount;
        public int SlashesCount => slashesCount;

        [Tooltip("Player.Damage * this")]
        [SerializeField] private float damage;
        public float Damage => damage;

        [Tooltip("Scale of each slash")]
        [SerializeField] private float slashSize;
        public float SlashSize => slashSize;

        [Tooltip("Delay before each slash")]
        [SerializeField] private float timeBetweenSlashes;
        public float TimeBetweenSlashes => timeBetweenSlashes;

        [Header("Random Rotation")]
        [Tooltip("Give each slash a random Z rotation")]
        [SerializeField] private bool randomRotation = true;
        public bool RandomRotation => randomRotation;

        [Header("Knock‑Back")]
        [Tooltip("Enable collider knock‑back on hit")]
        [SerializeField] private bool enableKickBack = false;
        public bool EnableKickBack => enableKickBack;

        [Header("Bounce Settings")]
        [Tooltip("How far to search for next enemy after each slash")]
        [SerializeField, Min(0f)] private float bounceRadius = 5f;
        public float BounceRadius => bounceRadius;

        [Tooltip("Chance (0–1) each slash hops to a new target; else stays on same")]
        [SerializeField, Range(0f,1f)] private float bounceChance = 0.5f;
        public float BounceChance => bounceChance;
    }
}
