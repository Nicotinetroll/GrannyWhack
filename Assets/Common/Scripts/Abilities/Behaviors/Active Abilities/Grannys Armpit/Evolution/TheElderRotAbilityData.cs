using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "The Elder Rot Ability Data", menuName = "October/Abilities/Evolution/The Elder Rot")]
    public class TheElderRotAbilityData : GenericAbilityData<TheElderRotAbilityLevel>
    {
        private void Awake()
        {
            type = AbilityType.TheElderRot;
        }

        private void OnValidate()
        {
            type = AbilityType.TheElderRot;
        }
    }

    [System.Serializable]
    public class TheElderRotAbilityLevel : AbilityLevel
    {
        [Tooltip("The size of the timezone")]
        [SerializeField, Min(0.1f)] float fieldRadius = 0.5f;
        public float FieldRadius => fieldRadius;

        [Tooltip("Enemies inside timezone will move slower")]
        [SerializeField, Min(0.1f)] float slowDownMultiplier = 0.5f;
        public float SlowDownMultiplier => slowDownMultiplier;

        [Tooltip("The damage of the timezone. Multiplier by Player damage.")]
        [SerializeField, Min(0.1f)] float damage = 1f;
        public float Damage => damage;

        [Tooltip("Enemy will take damage every 'damageCooldown' seconds")]
        [SerializeField, Min(0f)] float damageCooldown;
        public float DamageCooldown => damageCooldown;
    }
}