using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using UnityEngine;
using UnityEngine.Events;
using CartoonFX;

namespace OctoberStudio.Abilities
{
    public class FireballProjectileBehavior : MonoBehaviour
    {
        private static readonly int FIREBALL_LAUNCH_HASH = "Fireball Launch".GetHashCode();
        private static readonly int FIREBALL_EXPLOSION_HASH = "Fireball Explosion".GetHashCode();

        [Header("References")]
        [SerializeField] private Collider2D fireballCollider;
        [SerializeField] private GameObject visuals;

        [Header("VFX")]
        [SerializeField] private GameObject flashEffect;
        [SerializeField] private string hitEffectPoolName = "FireballHit";
        [SerializeField] private ParticleSystem projectileVFX;
        [SerializeField] private float hitEffectOffset = 0.1f;

        [Header("Settings")]
        [SerializeField] private float disableDelay = 0.5f;

        private IEasingCoroutine movementCoroutine;
        private IEasingCoroutine disableCoroutine;

        public float DamageMultiplier { get; set; }
        public float ExplosionRadius { get; set; }
        public float Lifetime { get; set; }
        public float Speed { get; set; }
        public float Size { get; set; }

        public event UnityAction<FireballProjectileBehavior> onFinished;

        public void Init()
        {
            transform.localScale = Vector3.one * Size * PlayerBehavior.Player.SizeMultiplier;

            var distance = Speed * Lifetime * PlayerBehavior.Player.DurationMultiplier;
            var selfDestructPosition = transform.position + transform.rotation * Vector3.up * distance;

            movementCoroutine = transform.DoPosition(selfDestructPosition, Lifetime / PlayerBehavior.Player.ProjectileSpeedMultiplier)
                .SetOnFinish(Explode);

            // Enable visuals
            visuals.SetActive(true);
            fireballCollider.enabled = true;

            // Play launch flash VFX
            if (flashEffect != null)
            {
                flashEffect.SetActive(true);
                var flashParticles = flashEffect.GetComponent<ParticleSystem>();
                if (flashParticles != null) flashParticles.Play();
            }

            // Play looping projectile effect
            if (projectileVFX != null)
                projectileVFX.Play();

            GameController.AudioManager.PlaySound(FIREBALL_LAUNCH_HASH);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out EnemyBehavior enemy))
            {
                movementCoroutine.Stop();
                Explode();
            }
        }

        private void Explode()
        {
            fireballCollider.enabled = false;
            visuals.SetActive(false);

            var enemies = StageController.EnemiesSpawner.GetEnemiesInRadius(transform.position, ExplosionRadius);
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].TakeDamage(PlayerBehavior.Player.Damage * DamageMultiplier);
            }

            // Spawn hit VFX from pool
            var hitVFX = StageController.PoolsManager.GetEntity(hitEffectPoolName);
            if (hitVFX != null)
            {
                hitVFX.transform.position = transform.position + Vector3.up * hitEffectOffset;
                hitVFX.transform.rotation = Quaternion.identity;
                hitVFX.SetActive(true);

                if (hitVFX.TryGetComponent(out CFXR_Effect fx))
                    fx.Initialize();
                else
                    hitVFX.GetComponent<ParticleSystem>()?.Play();
            }

            GameController.AudioManager.PlaySound(FIREBALL_EXPLOSION_HASH);

            disableCoroutine = EasingManager.DoAfter(disableDelay, () =>
            {
                gameObject.SetActive(false);
                onFinished?.Invoke(this);
            });
        }

        public void Clear()
        {
            movementCoroutine.StopIfExists();
            disableCoroutine.StopIfExists();

            gameObject.SetActive(false);

            visuals.SetActive(true);
            fireballCollider.enabled = true;
        }
    }
}
