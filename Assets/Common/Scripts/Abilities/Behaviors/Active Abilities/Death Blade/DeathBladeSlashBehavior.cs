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

        private IEasingCoroutine _wait, _colOff;

        public override void Init()
        {
            base.Init();

            transform.localScale = Vector3.one * Size * PlayerBehavior.Player.SizeMultiplier;
            slashCollider.enabled = true;
            _colOff = EasingManager.DoAfter(0.1f, () => slashCollider.enabled = false);

            _wait = EasingManager.DoAfter(duration, () =>
            {
                onFinished?.Invoke(this);
                Disable();
            });

            // restart all CFXR effects
            foreach (var fx in GetComponentsInChildren<CFXR_Effect>(true))
            {
                fx.gameObject.SetActive(true);
                fx.Initialize();
            }
        }

        public void Disable()
        {
            _wait.StopIfExists();
            _colOff.StopIfExists();
            slashCollider.enabled = true;
            gameObject.SetActive(false);
        }
    }
}