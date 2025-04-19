using System.Collections;
using System.Collections.Generic;
using CartoonFX;          // if you use CFXR_Effect
using OctoberStudio.Easing;
using OctoberStudio.Timeline;
using Unity.VisualScripting;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    [ExecuteAlways]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PoisonCloudBehavior : MonoBehaviour, IPoolable
    {
        [Header("References")]
        [SerializeField, Tooltip("Drag your CircleCollider2D here")]
        private CircleCollider2D _cloudCollider;

        [Header("Visual Tuning (edit‑time)")]
        [SerializeField, Tooltip("Art's native radius at scale=1")]
        private float effectBaseRadius = 0.5f;
        [SerializeField, Tooltip("Preview collider radius in editor")]
        private float previewRadius = 0.5f;

        // Runtime (set by the ability)
        public float Radius        { get; set; }
        public float Lifetime      { get; set; }
        public float Damage        { get; set; }
        public float TickInterval  { get; set; }
        public float SlowAmount    { get; set; }
        public float SlowDuration  { get; set; }

        const float buildUpDelay = 1f;
        private Coroutine _tickRoutine;
        private HashSet<EnemyBehavior> _handled = new();
        private Dictionary<EnemyBehavior, float> _nextTick = new();

        void Reset()
        {
            if (_cloudCollider == null)
            {
                _cloudCollider = GetComponent<CircleCollider2D>();
                if (_cloudCollider != null)
                    _cloudCollider.isTrigger = true;
            }
        }

        void OnValidate()
        {
            if (_cloudCollider == null) return;
            _cloudCollider.radius = previewRadius;
            float s = previewRadius / effectBaseRadius;
            transform.localScale = Vector3.one * s;
        }

        /// <summary>Called by PoolComponent.GetEntity()</summary>
        public void Init()
        {
            if (_cloudCollider == null)
                _cloudCollider = GetComponent<CircleCollider2D>();
            _cloudCollider.isTrigger = true;
            _cloudCollider.radius = Radius;

            float scale = (Radius / effectBaseRadius) * PlayerBehavior.Player.SizeMultiplier;
            transform.localScale = Vector3.one * scale;
            transform
                .DoLocalScale(Vector3.one * scale, 0.5f)
                .SetEasing(EasingType.SineOut);

            // restart any VFX
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

            _handled.Clear();
            _nextTick.Clear();
            _tickRoutine = StartCoroutine(CloudTick());
            StartCoroutine(AutoDestroy());
        }

        private IEnumerator CloudTick()
        {
            yield return new WaitForSeconds(buildUpDelay);

            while (true)
            {
                var hits = Physics2D.OverlapCircleAll(transform.position, Radius);
                float now = Time.time;

                foreach (var c in hits)
                {
                    if (!c.TryGetComponent(out EnemyBehavior e)) continue;

                    if (!_nextTick.TryGetValue(e, out float next) || now >= next)
                    {
                        _nextTick[e] = now + TickInterval;
                        e.TakeDamage(PlayerBehavior.Player.Damage * Damage);
                    }

                    if (_handled.Add(e))
                    {
                        var slow = new Effect(EffectType.Speed, 1f - SlowAmount);
                        e.AddEffect(slow);
                        StartCoroutine(RemoveEffectAfter(slow, e, SlowDuration));
                    }
                }

                yield return new WaitForSeconds(TickInterval);
            }
        }

        private IEnumerator RemoveEffectAfter(Effect effect, EnemyBehavior e, float duration)
        {
            yield return new WaitForSeconds(duration);
            e.RemoveEffect(effect);
        }

        private IEnumerator AutoDestroy()
        {
            yield return new WaitForSeconds(Lifetime);
            transform
                .DoLocalScale(Vector3.zero, 0.5f)
                .SetEasing(EasingType.SineIn)
                .SetOnFinish(() => gameObject.SetActive(false));

            if (_tickRoutine != null)
                StopCoroutine(_tickRoutine);
        }

        public void Release()
        {
            if (_tickRoutine != null)
                StopCoroutine(_tickRoutine);
            gameObject.SetActive(false);
        }

        public void New() => Release();
        public void Free() => Release();
    }
}
