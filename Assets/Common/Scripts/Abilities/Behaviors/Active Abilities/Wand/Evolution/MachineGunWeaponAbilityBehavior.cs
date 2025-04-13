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

        [Header("Prefabs")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject gunVisualPrefab;

        [Header("Gun Settings")]
        [SerializeField] private Vector3 gunOffset = new Vector3(0, -0.25f, 0);
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float kickbackDistance = 0.1f;
        [SerializeField] private float kickbackDuration = 0.08f;

        public GameObject ProjectilePrefab => projectilePrefab;

        private PoolComponent<MachineGunProjectileBehavior> projectilePool;
        private List<MachineGunProjectileBehavior> projectiles = new();

        private GameObject activeGunVisual;
        private Transform firePoint;
        private Transform weaponRenderer;
        private SpriteRenderer weaponSprite;

        private Coroutine abilityCoroutine;
        private IEasingCoroutine projectileCoroutine;

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

            if (gunVisualPrefab != null && activeGunVisual == null)
            {
                activeGunVisual = Instantiate(gunVisualPrefab, PlayerBehavior.CenterTransform);
                activeGunVisual.transform.localPosition = gunOffset;

                firePoint = activeGunVisual.transform.Find("Weapon renderer/FirePoint");
                weaponRenderer = activeGunVisual.transform.Find("Weapon renderer");

                if (weaponRenderer != null)
                    weaponSprite = weaponRenderer.GetComponent<SpriteRenderer>();

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

                    // 🔁 Smooth rotate gun to direction
                    if (activeGunVisual != null)
                    {
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

                        activeGunVisual.transform.rotation = Quaternion.RotateTowards(
                            activeGunVisual.transform.rotation,
                            targetRotation,
                            rotationSpeed * Time.deltaTime
                        );

                        // 🔄 Flip weapon Y based on direction
                        if (weaponSprite != null)
                        {
                            weaponSprite.flipY = direction.x < 0;
                        }
                    }

                    // 🔫 Fire when ready
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

                        GameController.AudioManager.PlaySound(MACHINE_GUN_PROJECTILE_LAUNCH_HASH);

                        // 🔥 Kickback effect
                        if (weaponRenderer != null)
                        {
                            Vector3 originalPos = weaponRenderer.localPosition;
                            Vector3 kickbackPos = originalPos - (Vector3)(direction.normalized * kickbackDistance);

                            EasingManager.DoLerp(kickbackDuration, t =>
                            {
                                weaponRenderer.localPosition = Vector3.LerpUnclamped(originalPos, kickbackPos, t);
                            }, () =>
                            {
                                // Ease back to original
                                EasingManager.DoLerp(kickbackDuration, t =>
                                {
                                    weaponRenderer.localPosition = Vector3.LerpUnclamped(kickbackPos, originalPos, t);
                                });
                            });
                        }
                    }
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
