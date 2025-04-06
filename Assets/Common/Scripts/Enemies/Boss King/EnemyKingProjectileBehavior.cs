using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    public class EnemyKingProjectileBehavior : SimpleEnemyProjectileBehavior
    {
        private static readonly int IS_FLYING_DOWN_BOOL = Animator.StringToHash("Is Flying Down");

        [Header("References")]
        [SerializeField] private Collider2D projectileCollider;
        [SerializeField] private GameObject visuals;
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem hitParticle;
        [SerializeField] private ParticleSystem explosionParticle;
        
        [Header("Explosion Damage")]
        [SerializeField] private float explosionRadius = 1.5f;
        [SerializeField] private float explosionDamage = 10f;
        [SerializeField] private float enemyDamageMultiplier = 2f; // ✅ all enemies take more
        [SerializeField] private LayerMask damageMask;

        
        [Header("Boss Damage")]
        [SerializeField] private float bossSelfDamageMultiplier = 2f;


        [Header("Explosion Settings")]
        [SerializeField] private float explodeDelay = 2f;
        [SerializeField] private float postExplosionDelay = 0.3f;

        [Header("Audio Names (from AudioDatabase)")]
        [SerializeField] private string bombImpactSoundName = "BossKing_BombImpact";
        [SerializeField] private string bombExplosionSoundName = "BossKing_BombExplosion";

        private Coroutine attackCoroutine;
        public new UnityAction<EnemyKingProjectileBehavior> onFinished;

        private WarningCircleBehavior warningCircle;
        private IEasingCoroutine easingCoroutine;
        private bool exploded;

        public override void Init(Vector2 position, Vector2 direction)
        {
            base.Init(position, direction);

            projectileCollider.enabled = false;
            visuals.SetActive(true);
            exploded = false;

            Vector3 upPosition = transform.position.SetY(CameraManager.TopBound + 2f);
            easingCoroutine = transform.DoPosition(upPosition, 0.3f)
                .SetEasing(EasingType.SineIn)
                .SetOnFinish(HideAndStartFall);
        }

        private void HideAndStartFall()
        {
            visuals.SetActive(false);
            attackCoroutine = StartCoroutine(AttackCoroutine());
        }

        private IEnumerator AttackCoroutine()
        {
            warningCircle = StageController.PoolsManager.GetEntity<WarningCircleBehavior>("Warning Circle");
            warningCircle.transform.position = PlayerBehavior.Player.transform.position;
            warningCircle.Play(1f, 0.3f, 100, null);
            warningCircle.Follow(PlayerBehavior.Player.transform, Vector3.zero, Time.deltaTime * 3);

            yield return new WaitForSeconds(2f);

            warningCircle.StopFollowing();
            Vector2 targetPosition = warningCircle.transform.position;

            transform.position = targetPosition.SetY(CameraManager.TopBound + 2f);
            visuals.SetActive(true);

            animator.SetBool(IS_FLYING_DOWN_BOOL, true);

            easingCoroutine = transform.DoPosition(targetPosition, 0.5f).SetOnFinish(() =>
            {
                animator.SetBool(IS_FLYING_DOWN_BOOL, false);
                projectileCollider.enabled = true;

                // 🔊 Impact sound
                GameController.AudioManager.PlaySound(bombImpactSoundName.GetHashCode());

                // 💥 Hit VFX
                hitParticle?.Play();

                if (warningCircle != null)
                {
                    warningCircle.gameObject.SetActive(false);
                    warningCircle = null;
                }

                easingCoroutine = EasingManager.DoAfter(0.3f, () =>
                {
                    projectileCollider.enabled = false;
                });

                // 💣 Schedule explosion after delay
                EasingManager.DoAfter(explodeDelay, Explode);
            });
        }

        private void Explode()
        {
            if (exploded) return;
            exploded = true;

            visuals.SetActive(false);

            // 💥 Explosion VFX
            if (explosionParticle != null)
            {
                explosionParticle.gameObject.SetActive(true);
                explosionParticle.transform.position = transform.position;
                explosionParticle.Play();
            }

            // 🔊 Explosion sound
            GameController.AudioManager.PlaySound(bombExplosionSoundName.GetHashCode());

            // ☠️ Deal damage
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageMask);

            foreach (var hit in hits)
            {
                // 👤 Player takes normal damage
                if (hit.TryGetComponent<PlayerBehavior>(out var player))
                {
                    player.TakeDamage(explosionDamage);
                    continue;
                }

                // 👾 All enemies take multiplied damage
                if (hit.TryGetComponent<EnemyBehavior>(out var enemy))
                {
                    float finalDamage = explosionDamage * enemyDamageMultiplier;
                    enemy.TakeDamage(finalDamage);
                }
            }

            // ⏳ Despawn after particle finishes
            float waitTime = explosionParticle != null ? explosionParticle.main.duration : 0.3f;

            EasingManager.DoAfter(waitTime, () =>
            {
                gameObject.SetActive(false);
                onFinished?.Invoke(this);
            });
        }







        public void Clear()
        {
            if (attackCoroutine != null)
                StopCoroutine(attackCoroutine);

            easingCoroutine.StopIfExists();

            if (warningCircle != null)
                warningCircle.gameObject.SetActive(false);

            gameObject.SetActive(false);
        }
    }
}
