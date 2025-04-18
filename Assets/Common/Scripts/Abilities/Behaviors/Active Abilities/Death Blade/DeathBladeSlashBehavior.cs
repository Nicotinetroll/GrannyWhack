// DeathBladeSlashBehavior.cs
using OctoberStudio.Easing;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Abilities
{
    public class DeathBladeSlashBehavior : ProjectileBehavior
    {
        [SerializeField] private Collider2D slashCollider;
        [SerializeField] private float duration;

        public float Size { get; set; }
        public UnityAction<DeathBladeSlashBehavior> onFinished;

        private IEasingCoroutine _waitingCoroutine;
        private IEasingCoroutine _colliderToggleCoroutine;

        public override void Init()
        {
            base.Init();

            // scale to match player + configured size
            transform.localScale = Vector3.one * Size * PlayerBehavior.Player.SizeMultiplier;

            // enable collider for a brief instant
            slashCollider.enabled = true;
            _colliderToggleCoroutine = EasingManager
                .DoAfter(0.1f, () => slashCollider.enabled = false);

            // wait out the remainder of duration then fire finished
            _waitingCoroutine = EasingManager
                .DoAfter(duration, () =>
                {
                    onFinished?.Invoke(this);
                    Disable();
                });
        }

        public void Disable()
        {
            _waitingCoroutine.StopIfExists();
            _colliderToggleCoroutine.StopIfExists();

            // reset and hide
            slashCollider.enabled = true;
            gameObject.SetActive(false);
        }
    }
}