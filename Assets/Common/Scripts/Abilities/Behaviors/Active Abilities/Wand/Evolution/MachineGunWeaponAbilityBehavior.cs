using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class MachineGunWeaponAbilityBehavior : AbilityBehavior<MachineGunWeaponAbilityData, MachineGunWeaponAbilityLevel>
    {
        public static readonly int MACHINE_GUN_PROJECTILE_LAUNCH_HASH = "Machine Gun Projectile Launch".GetHashCode();

        [SerializeField] GameObject projectilePrefab;
        public GameObject ProjectilePrefab => projectilePrefab;

        private PoolComponent<MachineGunProjectileBehavior> projectilePool;
        public List<MachineGunProjectileBehavior> projectiles = new();

        IEasingCoroutine projectileCoroutine;
        Coroutine abilityCoroutine;

        private float AbilityCooldown => AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;

        private void Awake()
        {
            projectilePool = new PoolComponent<MachineGunProjectileBehavior>("Machine Gun Projectile", projectilePrefab, 50);
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
            var lastTimeSpawned = Time.time - AbilityCooldown;

            while (true)
            {
                while (lastTimeSpawned + AbilityCooldown < Time.time)
                {
                    var spawnTime = lastTimeSpawned + AbilityCooldown;
                    var projectile = projectilePool.GetEntity();

                    var closestEnemy = StageController.EnemiesSpawner.GetClosestEnemy(PlayerBehavior.CenterPosition);
                    var direction = closestEnemy != null
                        ? (closestEnemy.Center - PlayerBehavior.CenterPosition).normalized
                        : Vector2.up;

                    var aliveDuration = Time.time - spawnTime;
                    var position = PlayerBehavior.CenterPosition +
                                   direction * aliveDuration * AbilityLevel.ProjectileSpeed * PlayerBehavior.Player.ProjectileSpeedMultiplier;

                    projectile.InitBounce(
                        position,
                        direction,
                        AbilityLevel.ProjectileSpeed * PlayerBehavior.Player.ProjectileSpeedMultiplier,
                        AbilityLevel.ProjectileLifetime,
                        AbilityLevel.Damage,
                        AbilityLevel.BounceCount,
                        AbilityLevel.BounceRadius
                    );

                    projectile.onFinished += OnProjectileFinished;
                    projectiles.Add(projectile);

                    lastTimeSpawned += AbilityCooldown;
                    GameController.AudioManager.PlaySound(MACHINE_GUN_PROJECTILE_LAUNCH_HASH);
                }

                yield return null;
            }
        }

        private void OnProjectileFinished(SimplePlayerProjectileBehavior projectile)
        {
            projectile.onFinished -= OnProjectileFinished;

            if (projectile is MachineGunProjectileBehavior mgProjectile)
            {
                projectiles.Remove(mgProjectile);
            }
        }

        private void Disable()
        {
            projectileCoroutine.StopIfExists();

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
