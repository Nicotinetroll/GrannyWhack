using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Machine Gun", menuName = "October/Abilities/Evolution/Machine Gun")]
    public class MachineGunWeaponAbilityData : GenericAbilityData<MachineGunWeaponAbilityLevel>
    {
        private void Awake()
        {
            type = AbilityType.MachineGun;
            isWeaponAbility = true;
        }

        private void OnValidate()
        {
            type = AbilityType.MachineGun;
            isWeaponAbility = true;
        }
    }

    [System.Serializable]
    public class MachineGunWeaponAbilityLevel : AbilityLevel
    {
        [Tooltip("Amount of time between attacks")]
        [SerializeField] private float abilityCooldown = 0.2f;
        public float AbilityCooldown => abilityCooldown;

        [Tooltip("Speed of the projectile")]
        [SerializeField] private float projectileSpeed = 15f;
        public float ProjectileSpeed => projectileSpeed;

        [Tooltip("Damage of projectiles: Player.Damage * Damage")]
        [SerializeField] private float damage = 1f;
        public float Damage => damage;

        [Tooltip("Size of projectiles")]
        [SerializeField] private float projectileSize = 1f;
        public float ProjectileSize => projectileSize;

        [Tooltip("Time projectiles stay alive")]
        [SerializeField] private float projectileLifetime = 3f;
        public float ProjectileLifetime => projectileLifetime;

        [Tooltip("How many enemies can the projectile bounce through")]
        [SerializeField] private int bounceCount = 2;
        public int BounceCount => bounceCount;

        [Tooltip("Max radius to search for next bounce target")]
        [SerializeField] private float bounceRadius = 4f;
        public float BounceRadius => bounceRadius;
    }
}