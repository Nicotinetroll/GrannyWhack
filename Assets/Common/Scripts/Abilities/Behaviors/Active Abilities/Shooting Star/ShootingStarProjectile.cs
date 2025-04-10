using OctoberStudio.Easing;
using UnityEngine;

namespace OctoberStudio
{
    public class ShootingStarProjectile : ProjectileBehavior
    {
        private IEasingCoroutine scaleEasing;

        [Header("VFX")]
        [SerializeField] private float hitOffset = 0.1f;
        [SerializeField] private string hitEffectPoolKey = "ShootingStarHitVFX";

        public void Spawn()
        {
            scaleEasing.StopIfExists();

            transform.localScale = Vector3.zero;
            scaleEasing = transform.DoLocalScale(Vector3.one * PlayerBehavior.Player.SizeMultiplier, 0.5f)
                .SetEasing(EasingType.SineOut);
        }

        public void Hide()
        {
            scaleEasing.StopIfExists();

            scaleEasing = transform.DoLocalScale(Vector3.zero, 0.5f)
                .SetEasing(EasingType.SineIn)
                .SetOnFinish(() =>
                {
                    gameObject.SetActive(false);
                });
        }

        public void Clear()
        {
            scaleEasing.StopIfExists();
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other.GetComponent<EnemyBehavior>();
            if (enemy == null) return;

            // 💥 Deal damage
            enemy.TakeDamage(PlayerBehavior.Player.Damage * DamageMultiplier);

            // 💫 Play hit effect
            var fx = StageController.PoolsManager.GetEntity(hitEffectPoolKey);
            if (fx != null)
            {
                fx.transform.position = transform.position + (transform.up * hitOffset);
                fx.transform.rotation = Quaternion.LookRotation(Vector3.forward, transform.up);
                fx.SetActive(true);

                if (fx.TryGetComponent(out ParticleSystem ps))
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play();
                }
            }

            // ❌ NO Hide() — we keep the projectile alive
        }
    }
}
