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
        [SerializeField] private float kickbackDistance = 0.2f;
        [SerializeField] private float kickbackReturnTime = 0.1f;

        private GameObject activeGunVisual;
        private Transform firePoint;
        private SpriteRenderer weaponSprite;

        private PoolComponent<WandProjectileBehavior> projectilePool;
        private List<WandProjectileBehavior> projectiles = new();

        private Coroutine abilityCoroutine;
        private IEasingCoroutine projectileCoroutine;

        private Vector3 currentKickback = Vector3.zero;

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

                Transform spriteTransform = activeGunVisual.transform.Find("Weapon renderer");
                if (spriteTransform != null)
                    weaponSprite = spriteTransform.GetComponent<SpriteRenderer>();
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

                    // 🔁 Rotate to face direction
                    if (activeGunVisual != null)
                    {
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
                        activeGunVisual.transform.rotation = Quaternion.RotateTowards(
                            activeGunVisual.transform.rotation,
                            targetRotation,
                            720f * Time.deltaTime
                        );
                    }

                    if (weaponSprite != null)
                    {
                        weaponSprite.flipY = direction.x < 0;
                    }

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

                        // 💥 Kickback!
                        ApplyKickback(-direction);
                    }
                }

                yield return null;
            }
        }

        private void ApplyKickback(Vector2 direction)
        {
            if (activeGunVisual == null) return;

            Vector3 start = gunOffset;
            Vector3 kickbackTarget = gunOffset + (Vector3)(direction.normalized * kickbackDistance);

            activeGunVisual.transform.localPosition = kickbackTarget;

            // Ease back to original offset
            EasingManager.DoLerp(kickbackReturnTime, t =>
            {
                activeGunVisual.transform.localPosition = Vector3.Lerp(kickbackTarget, start, t);
            });
        }

        private void OnProjectileFinished(SimplePlayerProjectileBehavior projectile)
        {
            projectile.onFinished -= OnProjectileFinished;

            if (projectile is WandProjectileBehavior wandProjectile)
                projectiles.Remove(wandProjectile);
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
