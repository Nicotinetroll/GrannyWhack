using UnityEngine;

namespace OctoberStudio
{
    /// <summary>
    /// A static enemy that never moves, cannot be knocked back, and deals no damage.
    /// </summary>
    public class StaticEnemyBehavior : EnemyBehavior
    {
        protected override void Awake()
        {
            // Call the base class Awake() to initialize materials, UI, etc.
            base.Awake();
            // Disable the shadow (if any)
            if (shadowSprite != null)
                shadowSprite.gameObject.SetActive(false);
        }

        public override void Play()
        {
            // Call the base Play() to set HP and other parameters.
            base.Play();

            // Immediately set movement off.
            IsMoving = false;
            // Ensure the collider is in trigger mode so it doesn’t physically interact with other enemies.
            if (enemyCollider != null)
                enemyCollider.isTrigger = true;
        }

        protected override void Update()
        {
            // Do nothing so that this enemy remains static at its spawn point.
        }

        // Override KickBack so the enemy is never knocked around.
        public override void KickBack(Vector3 position)
        {
            // Do nothing.
        }

        // Override GetDamage so this enemy deals no damage.
        public override float GetDamage()
        {
            return 0f;
        }
    }
}