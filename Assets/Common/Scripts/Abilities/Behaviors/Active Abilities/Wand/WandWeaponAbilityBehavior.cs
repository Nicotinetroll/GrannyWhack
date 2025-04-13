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

        [Header("Prefabs")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject gunVisualPrefab;

        [Header("Gun Settings")]
        [SerializeField] private Vector3 gunOffset = new Vector3(0, -0.25f, 0);

        private GameObject activeGunVisual;
        private Transform firePoint;

        private PoolComponent<WandProjectileBehavior> projectilePool;
        private List<WandProjectileBehavior> projectiles = new();

        private Coroutine abilityCoroutine;
        private IEasingCoroutine projectileCoroutine;

        public GameObject ProjectilePrefab => projectilePrefab;
        private float AbilityCooldown => AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;

        private void Awake()
        {
            projectilePool = new PoolComponent<WandProjectileBehavior>("Wand Projectile", ProjectilePrefab, 50);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);

            if (abilityCoroutine != null)
                Disable();

            if (gunVisualPrefab != null && activeGunVisual == null)
            {
                activeGunVisual = Instantiate(gunVisualPrefab, PlayerBehavior.CenterTransform);
                activeGunVisual.transform.localPosition = gunOffset;

                firePoint = activeGunVisual.transform.Find("Weapon renderer/FirePoint");
                if (firePoint == null)
                    Debug.LogWarning("FirePoint not found on gun visual prefab.");
            }

            abilityCoroutine = StartCoroutine(AbilityCoroutine());
        }

        private IEnumerator AbilityCoroutine()
        {
            float lastTimeSpawned = Time.time - AbilityCooldown;

            while (true)
            {
                var closestEnemy = StageController.EnemiesSpawner.GetClosestEnemy(PlayerBehavior.CenterPosition);

                if (closestEnemy != null && firePoint != null)
                {
                    Vector2 fireOrigin = firePoint.position;
                    Vector2 direction = (closestEnemy.Center - fireOrigin).normalized;

                    // 🔄 Pre-aim every frame toward target
                    if (activeGunVisual != null)
                    {
                        float desiredAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        Quaternion targetRotation = Quaternion.Euler(0, 0, desiredAngle);
                        activeGunVisual.transform.rotation = Quaternion.RotateTowards(
                            activeGunVisual.transform.rotation,
                            targetRotation,
                            720f * Time.deltaTime // 🔁 Up to 720°/sec rotation speed
                        );
                    }

                    // 🔫 Fire if ready
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

            if (activeGunVisual != null)
            {
                Destroy(activeGunVisual);
                activeGunVisual = null;
            }

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
