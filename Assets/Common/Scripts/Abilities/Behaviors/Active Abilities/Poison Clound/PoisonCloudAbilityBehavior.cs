using System.Collections;
using UnityEngine;
using OctoberStudio.Pool;
using OctoberStudio.Easing;
using OctoberStudio.Extensions;

namespace OctoberStudio.Abilities
{
    public class PoisonCloudAbilityBehavior 
        : AbilityBehavior<PoisonCloudAbilityData, PoisonCloudAbilityLevel>
    {
        public static readonly int POISON_CLOUD_SPAWN_HASH =
            "Poison Cloud Spawn".GetHashCode();

        [Header("Prefabs")]
        [SerializeField] private GameObject fallingPrefab;    // your falling‑from‑sky prefab
        [SerializeField] private GameObject cloudEffectPrefab;// your existing poison‑cloud prefab

        private PoolComponent<PoisonCloudFallingBehavior> _fallPool;
        private PoolComponent<PoisonCloudBehavior>        _cloudPool;
        private Coroutine                                _abilityCoroutine;

        void Awake()
        {
            _fallPool  = new PoolComponent<PoisonCloudFallingBehavior>(
                             "PoisonCloudFalling", fallingPrefab, 6);
            _cloudPool = new PoolComponent<PoisonCloudBehavior>(
                             "PoisonCloud",      cloudEffectPrefab, 10);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);
            if (_abilityCoroutine != null) StopCoroutine(_abilityCoroutine);
            _abilityCoroutine = StartCoroutine(AbilityLoop());
        }

        private IEnumerator AbilityLoop()
        {
            var lvl     = AbilityLevel;
            float cd    = lvl.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;
            float delay = lvl.TimeBetweenClouds * PlayerBehavior.Player.CooldownMultiplier;
            float total = lvl.CloudsCount * delay;

            while (true)
            {
                for (int i = 0; i < lvl.CloudsCount; i++)
                {
                    // random target in viewport
                    Vector3 vp        = new Vector3(Random.value, Random.value, Camera.main.nearClipPlane);
                    Vector3 targetPos = Camera.main.ViewportToWorldPoint(vp);

                    // 1) spawn fall visual
                    var fall = _fallPool.GetEntity();
                    fall.OnFinished += OnFallComplete;
                    fall.Init(targetPos, lvl.FallSpeed);

                    // 2) play sound immediately
                    GameController.AudioManager.PlaySound(POISON_CLOUD_SPAWN_HASH);

                    yield return new WaitForSeconds(delay);
                }

                float rem = cd - total;
                if (rem < 0.1f) rem = 0.1f;
                yield return new WaitForSeconds(rem);
            }
        }

        private void OnFallComplete(PoisonCloudFallingBehavior fall)
        {
            fall.OnFinished -= OnFallComplete;

            // now spawn the actual poison cloud at impact point
            Vector3 pos = fall.transform.position;
            var cloud = _cloudPool.GetEntity();
            cloud.transform.position = pos;
            var lvl = AbilityLevel;
            cloud.Radius       = lvl.Radius;
            cloud.Lifetime     = lvl.CloudLifetime;
            cloud.Damage       = lvl.Damage;
            cloud.TickInterval = lvl.TickInterval;
            cloud.SlowAmount   = lvl.SlowAmount;
            cloud.SlowDuration = lvl.SlowDuration;
            cloud.Init();
        }

        public override void Clear()
        {
            if (_abilityCoroutine != null) StopCoroutine(_abilityCoroutine);
            _fallPool.Destroy();
            _cloudPool.Destroy();
            base.Clear();
        }
    }
}
