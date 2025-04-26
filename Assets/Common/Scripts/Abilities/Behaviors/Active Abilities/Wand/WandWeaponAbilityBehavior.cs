using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class WandWeaponAbilityBehavior : AbilityBehavior<WoodenWandWeaponAbilityData, WoodenWandWeaponAbilityLevel>
    {
        public static readonly int WAND_PROJECTILE_LAUNCH_HASH = "Wand Projectile Launch".GetHashCode();

        [Header("Projectile Prefab")]
        [SerializeField] private GameObject projectilePrefab;

        private PoolComponent<WandProjectileBehavior> projectilePool;
        private List<WandProjectileBehavior> projectiles = new();

        private Coroutine abilityCoroutine;

        private float AbilityCooldown => AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;

        private void Awake()
        {
            projectilePool = new PoolComponent<WandProjectileBehavior>("Wand Projectile", projectilePrefab, 50);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);

            if (abilityCoroutine != null)
                Disable();

            abilityCoroutine = StartCoroutine(AbilityCoroutine());
        }

        private IEnumerator AbilityCoroutine()
        {
            float lastTimeSpawned = Time.time - AbilityCooldown;

            while (true)
            {
                var closestEnemy = StageController.EnemiesSpawner.GetClosestEnemy(PlayerBehavior.CenterPosition);

                if (closestEnemy != null)
                {
                    Vector2 fireOrigin = PlayerBehavior.CenterPosition;
                    Vector2 direction = (closestEnemy.Center - fireOrigin).normalized;

                    if (Time.time >= lastTimeSpawned + AbilityCooldown)
                    {
                        var projectile = projectilePool.GetEntity();

                        projectile.InitBounce(
                            fireOrigin,
                            direction,
                            AbilityLevel.ProjectileSpeed * PlayerBehavior.Player.ProjectileSpeedMultiplier,
                            AbilityLevel.ProjectileLifetime,
                            AbilityLevel.Damage,
                            AbilityLevel.BounceCount,
                            AbilityLevel.BounceRadius
                        );

                        projectile.onFinished += OnProjectileFinished;
                        projectiles.Add(projectile);

                        lastTimeSpawned = Time.time;
                        GameController.AudioManager.PlaySound(WAND_PROJECTILE_LAUNCH_HASH);
                    }
                }

                yield return null;
            }
        }

        private void OnProjectileFinished(SimplePlayerProjectileBehavior projectile)
        {
            projectile.onFinished -= OnProjectileFinished;

            if (projectile is WandProjectileBehavior wandProjectile)
                projectiles.Remove(wandProjectile);
        }

        private void Disable()
        {
            foreach (var proj in projectiles)
                proj.gameObject.SetActive(false);
            projectiles.Clear();

            if (abilityCoroutine != null)
                StopCoroutine(abilityCoroutine);
        }

        public override void Clear()
        {
            Disable();
            base.Clear();
        }
    }
}