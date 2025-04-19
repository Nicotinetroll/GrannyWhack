// PoisonCloudBehavior.cs
using System.Collections;
using CartoonFX;
using OctoberStudio.Easing;
using OctoberStudio.Timeline;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    [ExecuteAlways]
    public class PoisonCloudBehavior : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Drop your CircleCollider2D here to preview the cloud radius")]
        private CircleCollider2D _cloudCollider;

        [Header("Visual Tuning (edit‑time)")]
        [Tooltip("The art was authored to cover this radius at scale = 1")]
        [SerializeField] private float effectBaseRadius = 0.5f;
        [Tooltip("Preview collider radius in the editor")]
        [SerializeField] private float previewRadius = 0.5f;

        [Header("Runtime Settings (set by AbilityBehavior)")]
        public float Radius { get; set; }
        public float Lifetime { get; set; }
        public float Damage { get; set; }
        public float TickInterval { get; set; }
        public float SlowAmount { get; set; }
        public float SlowDuration { get; set; }

        [Header("Timing")]
        [Tooltip("Delay before the first damage tick")]
        [SerializeField] private float damageStartDelay = 1f;

        private IEasingCoroutine _scaleIn, _scaleOut;
        private Coroutine _tickRoutine;

        void Reset()
        {
            if (_cloudCollider == null)
            {
                _cloudCollider = GetComponent<CircleCollider2D>();
                if (_cloudCollider != null) _cloudCollider.isTrigger = true;
            }
        }

        void OnValidate()
        {
            if (_cloudCollider != null)
            {
                _cloudCollider.radius = previewRadius;
                float s = previewRadius / effectBaseRadius;
                transform.localScale = Vector3.one * s;
            }
        }

        public void Init()
        {
            if (_cloudCollider == null)
                _cloudCollider = GetComponent<CircleCollider2D>();

            _cloudCollider.isTrigger = true;
            _cloudCollider.radius = Radius;

            float scaleFactor = (Radius / effectBaseRadius) * PlayerBehavior.Player.SizeMultiplier;
            transform.localScale = Vector3.one * scaleFactor;
            _scaleIn = transform
                .DoLocalScale(Vector3.one * scaleFactor, 0.5f)
                .SetEasing(EasingType.SineOut);

            // replay VFX
            foreach (var fx in GetComponentsInChildren<CFXR_Effect>(true))
            {
                fx.gameObject.SetActive(true);
                fx.Initialize();
            }
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }

            // start ticking damage after a delay
            _tickRoutine = StartCoroutine(TickDamage());

            StartCoroutine(AutoDestroy());
        }

        private IEnumerator TickDamage()
        {
            // wait build‑up time first
            yield return new WaitForSeconds(damageStartDelay);

            while (true)
            {
                var hits = Physics2D.OverlapCircleAll(transform.position, Radius);
                foreach (var c in hits)
                {
                    if (c.TryGetComponent<EnemyBehavior>(out var enemy))
                    {
                        enemy.TakeDamage(PlayerBehavior.Player.Damage * Damage);

                        var effect = new Effect(EffectType.Speed, 1f - SlowAmount);
                        enemy.AddEffect(effect);
                        StartCoroutine(RemoveSlowAfter(effect, enemy, SlowDuration));
                    }
                }
                yield return new WaitForSeconds(TickInterval);
            }
        }

        private IEnumerator RemoveSlowAfter(Effect effect, EnemyBehavior enemy, float duration)
        {
            yield return new WaitForSeconds(duration);
            enemy.RemoveEffect(effect);
        }

        private IEnumerator AutoDestroy()
        {
            yield return new WaitForSeconds(Lifetime);
            _scaleOut = transform
                .DoLocalScale(Vector3.zero, 0.5f)
                .SetEasing(EasingType.SineIn)
                .SetOnFinish(() => gameObject.SetActive(false));

            if (_tickRoutine != null)
                StopCoroutine(_tickRoutine);
        }
    }
}
