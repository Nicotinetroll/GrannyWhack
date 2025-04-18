// DeathBladeSlashBehavior.cs
using OctoberStudio.Easing;
using UnityEngine;
using UnityEngine.Events;
using CartoonFX;

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

            // 1) scale + collider burst (same as before)
            transform.localScale = Vector3.one * Size * PlayerBehavior.Player.SizeMultiplier;
            slashCollider.enabled = true;
            _colliderToggleCoroutine = EasingManager
                .DoAfter(0.1f, () => slashCollider.enabled = false);

            _waitingCoroutine = EasingManager
                .DoAfter(duration, () =>
                {
                    onFinished?.Invoke(this);
                    Disable();
                });

            // 2) re‐activate & restart ANY CFXR_Effect under me
            //    include disabled ones by passing `true`
            foreach (var fx in GetComponentsInChildren<CartoonFX.CFXR_Effect>(true))
            {
                fx.gameObject.SetActive(true);  // undo ClearBehavior’s disable
                fx.Initialize();                // restart the particle system
            }
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