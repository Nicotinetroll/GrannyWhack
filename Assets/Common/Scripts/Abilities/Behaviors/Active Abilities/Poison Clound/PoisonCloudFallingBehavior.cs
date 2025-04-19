using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Abilities
{
    [RequireComponent(typeof(Transform))]
    public class PoisonCloudFallingBehavior : MonoBehaviour, IPoolable
    {
        [SerializeField] float defaultSpeed       = 6f;
        [SerializeField] GameObject explosionPrefab;
        public event UnityAction<PoisonCloudFallingBehavior> OnFinished;

        private IEasingCoroutine _move;

        public void Init(Vector3 targetPos, float fallSpeed)
        {
            float spawnY = Camera.main.orthographicSize * 2f + targetPos.y;
            transform.position = new Vector3(targetPos.x, spawnY, targetPos.z);

            float speed    = fallSpeed > 0f ? fallSpeed : defaultSpeed;
            float distance = Vector3.Distance(transform.position, targetPos);
            float duration = distance / speed;

            _move?.StopIfExists();
            _move = transform
                .DoPosition(targetPos, duration)
                .SetOnFinish(() => Explode(targetPos));
        }

        private void Explode(Vector3 at)
        {
            // 1) explosion VFX
            if (explosionPrefab != null)
                Instantiate(explosionPrefab, at, Quaternion.identity);

            // 2) clone the trail so it can finish independently
            var trail = GetComponentInChildren<TrailRenderer>();
            if (trail != null)
            {
                var clone = Instantiate(
                    trail.gameObject,
                    at,
                    trail.transform.rotation);
                var cloneTrail = clone.GetComponent<TrailRenderer>();
                cloneTrail.emitting = false;
                Destroy(clone, cloneTrail.time + 0.1f);

                // **CRUCIAL** clear the original so it records fresh points next fall
                trail.Clear();
            }

            // 3) clone & finish any ParticleSystems
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            {
                var cloneGO = Instantiate(ps.gameObject, at, ps.transform.rotation);
                var clonePS = cloneGO.GetComponent<ParticleSystem>();
                clonePS.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                var main = clonePS.main;
                float life = main.duration + main.startLifetime.constantMax;
                Destroy(cloneGO, life + 0.1f);
            }

            // 4) hand off to the ability to spawn the poison cloud
            OnFinished?.Invoke(this);

            // 5) recycle this falling object
            Free();
        }

        public void Free()
        {
            _move?.StopIfExists();
            gameObject.SetActive(false);
        }

        public void New()
        {
            // nothing special on creation
        }
    }
}
