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
        // ✅ Use exact string names from AudioDatabase
        private static readonly int FIREBALL_LAUNCH_HASH = "Fireball Launch".GetHashCode();
        private static readonly int FIREBALL_EXPLOSION_HASH = "Fireball Explosion".GetHashCode();

        [Header("References")]
        [SerializeField] private Collider2D fireballCollider;
        [SerializeField] private GameObject visuals;

        [Header("Settings")]
        [SerializeField] private string explosionPoolName = "FireballExplosion"; // 👈 Name used in PoolsManager

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

            visuals.SetActive(true);
            fireballCollider.enabled = true;

            // ✅ Play correct launch sound
            GameController.AudioManager.PlaySound(FIREBALL_LAUNCH_HASH);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other.GetComponent<EnemyBehavior>();
            if (enemy != null)
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

            // ✅ Spawn explosion VFX via pool
            var fxGO = StageController.PoolsManager.GetEntity(explosionPoolName);
            if (fxGO != null)
            {
                fxGO.transform.position = transform.position;
                fxGO.transform.rotation = Quaternion.identity;
                fxGO.SetActive(true);

                if (fxGO.TryGetComponent<CFXR_Effect>(out var fx))
                    fx.Initialize();
                else
                    fxGO.GetComponent<ParticleSystem>()?.Play();
            }

            // ✅ Play correct explosion sound
            GameController.AudioManager.PlaySound(FIREBALL_EXPLOSION_HASH);

            disableCoroutine = EasingManager.DoAfter(1f, () =>
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
