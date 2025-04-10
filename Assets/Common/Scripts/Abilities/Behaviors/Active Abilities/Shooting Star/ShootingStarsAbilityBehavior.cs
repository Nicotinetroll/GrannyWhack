using OctoberStudio.Easing;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class ShootingStarsAbilityBehavior : AbilityBehavior<ShootingStarAbilityData, ShootingStartAbilityLevel>
    {
        public static readonly int SHOOTING_STARS_LAUNCH_HASH = "Shooting Stars Launch".GetHashCode();

        [SerializeField] GameObject starPrefab;
        public GameObject StarPrefab => starPrefab;

        private PoolComponent<ShootingStarProjectile> projectilesPool;
        private Coroutine abilityCoroutine;

        private float angle = 0f;
        private float radiusMultiplier = 1f;
        private List<ShootingStarProjectile> activeStars = new();

        private void Awake()
        {
            projectilesPool = new PoolComponent<ShootingStarProjectile>("Shooting Star Ability Projectile", starPrefab, 6);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);

            if (abilityCoroutine != null) StopCoroutine(abilityCoroutine);

            DeactivateAllStars();
            activeStars.Clear();

            abilityCoroutine = StartCoroutine(AbilityCoroutine());
        }

        private IEnumerator AbilityCoroutine()
        {
            while (true)
            {
                float lifetime = AbilityLevel.ProjectileLifetime * PlayerBehavior.Player.DurationMultiplier;
                float cooldown = AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;

                activeStars.Clear();

                // Spawn stars
                for (int i = 0; i < AbilityLevel.ProjectilesCount; i++)
                {
                    var star = projectilesPool.GetEntity();

                    star.Init();
                    star.DamageMultiplier = AbilityLevel.Damage;
                    star.KickBack = true;
                    star.Spawn();

                    activeStars.Add(star);
                }

                GameController.AudioManager.PlaySound(SHOOTING_STARS_LAUNCH_HASH);

                // Animate radius expand
                EasingManager.DoFloat(0f, 1f, 0.5f, value => radiusMultiplier = value)
                    .SetEasing(EasingType.SineOut);

                yield return new WaitForSeconds(lifetime - 0.5f);

                // Shrink radius and hide stars
                foreach (var star in activeStars)
                    star.Hide();

                EasingManager.DoFloat(1f, 0f, 0.5f, value => radiusMultiplier = value)
                    .SetEasing(EasingType.SineOut);

                float delay = cooldown - lifetime;
                if (delay < 0.5f) delay = 0.5f;

                yield return new WaitForSeconds(delay);
            }
        }

        private void LateUpdate()
        {
            transform.position = PlayerBehavior.CenterPosition;

            angle += AbilityLevel.AngularSpeed * PlayerBehavior.Player.ProjectileSpeedMultiplier * Time.deltaTime;

            for (int i = 0; i < activeStars.Count; i++)
            {
                float projectileAngle = 360f / activeStars.Count * i;
                projectileAngle += angle;

                var star = activeStars[i];
                star.transform.localPosition = transform.position + Quaternion.Euler(0, 0, projectileAngle) * Vector3.up * AbilityLevel.Radius * radiusMultiplier * PlayerBehavior.Player.SizeMultiplier;
            }
        }

        public override void Clear()
        {
            if (abilityCoroutine != null) StopCoroutine(abilityCoroutine);
            abilityCoroutine = null;

            DeactivateAllStars();
            activeStars.Clear();

            projectilesPool.Destroy();

            base.Clear();
        }

        private void DeactivateAllStars()
        {
            foreach (var star in activeStars)
                star.Clear();
        }
    }
}
