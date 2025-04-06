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

            // 🫥 Hide bomb visuals instantly
            visuals.SetActive(false);

            // 💣 Play explosion VFX
            if (explosionParticle != null)
            {
                explosionParticle.gameObject.SetActive(true); // In case it was disabled
                explosionParticle.transform.position = transform.position;
                explosionParticle.Play();

                // ⏳ Wait for particle duration to finish before deactivating bomb object
                float waitTime = explosionParticle.main.duration;
                EasingManager.DoAfter(waitTime, () =>
                {
                    gameObject.SetActive(false);
                    onFinished?.Invoke(this);
                });
            }
            else
            {
                // Fallback if no particle assigned
                gameObject.SetActive(false);
                onFinished?.Invoke(this);
            }

            // 🔊 Explosion SFX
            GameController.AudioManager.PlaySound(bombExplosionSoundName.GetHashCode());
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
