using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Wooden Wand Data", menuName = "October/Abilities/Active/Wooden Wand")]
    public class WoodenWandWeaponAbilityData : GenericAbilityData<WoodenWandWeaponAbilityLevel>
    {
        private void Awake()
        {
            type = AbilityType.WoodenWand;
            isWeaponAbility = true;
        }

        private void OnValidate()
        {
            type = AbilityType.WoodenWand;
            isWeaponAbility = true;
        }
    }

    [System.Serializable]
    public class WoodenWandWeaponAbilityLevel : AbilityLevel
    {
        [Header("Cooldown Settings")]
        [Tooltip("Seconds between attacks")]
        [SerializeField] private float abilityCooldown;
        public float AbilityCooldown => abilityCooldown;

        [Header("Projectile Settings")]
        [Tooltip("Projectile flying speed")]
        [SerializeField] private float projectileSpeed;
        public float ProjectileSpeed => projectileSpeed;

        [Tooltip("Projectile lifetime in seconds")]
        [SerializeField] private float projectileLifetime;
        public float ProjectileLifetime => projectileLifetime;

        [Tooltip("Projectile damage multiplier (Player.Damage * Damage)")]
        [SerializeField] private float damage;
        public float Damage => damage;

        [Tooltip("Projectile size multiplier")]
        [SerializeField] private float projectileSize;
        public float ProjectileSize => projectileSize;

        [Header("Bouncing Settings")]
        [Tooltip("How many bounces the projectile can do")]
        [SerializeField] private int bounceCount;
        public int BounceCount => bounceCount;

        [Tooltip("Radius to search for next enemy to bounce")]
        [SerializeField] private float bounceRadius = 5f;
        public float BounceRadius => bounceRadius;
    }
}