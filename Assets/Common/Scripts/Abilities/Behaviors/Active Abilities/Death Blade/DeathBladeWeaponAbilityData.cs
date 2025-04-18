// DeathBladeWeaponAbilityData.cs
using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "DeathBladeWeaponAbilityData", menuName = "October/Abilities/Active/Death Blade")]
    public class DeathBladeWeaponAbilityData : GenericAbilityData<DeathBladeWeaponAbilityLevel>
    {
        private void Awake()
        {
            type = AbilityType.DeathBlade;
        }

        private void OnValidate()
        {
            type = AbilityType.DeathBlade;
        }
    }

    [System.Serializable]
    public class DeathBladeWeaponAbilityLevel : AbilityLevel
    {
        [Tooltip("Amount of time between attacks")]
        [SerializeField] private float abilityCooldown;
        public float AbilityCooldown => abilityCooldown;

        [Tooltip("Amount of slashes in the attack")]
        [SerializeField] private int slashesCount;
        public int SlashesCount => slashesCount;

        [Tooltip("Damage of slashes calculates like this: Player.Damage * Damage")]
        [SerializeField] private float damage;
        public float Damage => damage;

        [Tooltip("Size of slash")]
        [SerializeField] private float slashSize;
        public float SlashSize => slashSize;

        [Tooltip("Delay before each slash")]
        [SerializeField] private float timeBetweenSlashes;
        public float TimeBetweenSlashes => timeBetweenSlashes;
    }
}