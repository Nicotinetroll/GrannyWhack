// ----------------------------
// WandWeaponAbilityBehavior.cs
// ----------------------------
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

        [SerializeField] GameObject projectilePrefab;
        public GameObject ProjectilePrefab => projectilePrefab;

        private PoolComponent<WandProjectileBehavior> projectilePool;
        public List<WandProjectileBehavior> projectiles = new List<WandProjectileBehavior>();

        IEasingCoroutine projectileCoroutine;
        Coroutine abilityCoroutine;

        private float AbilityCooldown => AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;

        private void Awake()
        {
            projectilePool = new PoolComponent<WandProjectileBehavior>("Wand Projectile", ProjectilePrefab, 50);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);

            if (abilityCoroutine != null) Disable();

            abilityCoroutine = StartCoroutine(AbilityCoroutine());
        }

        private IEnumerator AbilityCoroutine()
        {
            var lastTimeSpawned = Time.time - AbilityCooldown;

            while (true)
            {
                while (lastTimeSpawned + AbilityCooldown < Time.time)
                {
                    var spawnTime = lastTimeSpawned + AbilityCooldown;
                    var projectile = projectilePool.GetEntity();
                    var closestEnemy = StageController.EnemiesSpawner.GetClosestEnemy(PlayerBehavior.CenterPosition);

                    var direction = Vector2.up;
                    if (closestEnemy != null)
                    {
                        direction = closestEnemy.Center - PlayerBehavior.CenterPosition;
                        direction.Normalize();
                    }

                    var aliveDuration = Time.time - spawnTime;
                    var position = PlayerBehavior.CenterPosition + direction * aliveDuration * AbilityLevel.ProjectileSpeed * PlayerBehavior.Player.ProjectileSpeedMultiplier;

                    projectile.InitBounce(
                        position,
                        direction,
                        AbilityLevel.ProjectileSpeed * PlayerBehavior.Player.ProjectileSpeedMultiplier,
                        AbilityLevel.ProjectileLifetime,
                        AbilityLevel.Damage,
                        AbilityLevel.BounceCount, // ✅ First time: pass bounceCount
                        AbilityLevel.BounceRadius
                    );

                    projectile.onFinished += OnProjectileFinished;
                    projectiles.Add(projectile);

                    lastTimeSpawned += AbilityCooldown;
                    GameController.AudioManager.PlaySound(WAND_PROJECTILE_LAUNCH_HASH);
                }
                yield return null;
            }
        }

        private void OnProjectileFinished(SimplePlayerProjectileBehavior projectile)
        {
            projectile.onFinished -= OnProjectileFinished;
            if (projectile is WandProjectileBehavior wandProjectile)
            {
                projectiles.Remove(wandProjectile);
            }
        }

        private void Disable()
        {
            projectileCoroutine.StopIfExists();
            foreach (var proj in projectiles)
                proj.gameObject.SetActive(false);
            projectiles.Clear();
            StopCoroutine(abilityCoroutine);
        }

        public override void Clear()
        {
            Disable();
            base.Clear();
        }
    }
}