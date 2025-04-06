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
        private static readonly int BOSSKING_IMPACT_HASH = "BossKing_BombImpact".GetHashCode();

        [SerializeField] Collider2D projectileCollider;
        [SerializeField] GameObject visuals;
        [SerializeField] Animator animator;
        [SerializeField] ParticleSystem hitParticle;

        private Coroutine attackCoroutine;
        public new UnityAction<EnemyKingProjectileBehavior> onFinished;

        private WarningCircleBehavior warningCircle;
        private IEasingCoroutine easingCoroutine;

        public override void Init(Vector2 position, Vector2 direction)
        {
            base.Init(position, direction);

            projectileCollider.enabled = false;

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

                // 🔊 Play impact sound
                GameController.AudioManager.PlaySound(BOSSKING_IMPACT_HASH);

                // 💥 Play hit VFX
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
            });

            yield return new WaitForSeconds(2f);

            gameObject.SetActive(false);
            onFinished?.Invoke(this);

            attackCoroutine = null;
        }

        public void Clear()
        {
            if (attackCoroutine != null)
                StopCoroutine(attackCoroutine);

            if (warningCircle != null)
                warningCircle.gameObject.SetActive(false);

            easingCoroutine.StopIfExists();

            gameObject.SetActive(false);
        }
    }
}
