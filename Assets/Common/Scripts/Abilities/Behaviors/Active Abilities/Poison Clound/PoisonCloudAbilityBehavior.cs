// PoisonCloudAbilityBehavior.cs
using System.Collections;
using OctoberStudio.Pool;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class PoisonCloudAbilityBehavior 
        : AbilityBehavior<PoisonCloudAbilityData, PoisonCloudAbilityLevel>
    {
        [SerializeField] private GameObject cloudPrefab;
        private PoolComponent<PoisonCloudBehavior> _pool;
        private Coroutine _abilityCoroutine;

        void Awake()
        {
            _pool = new PoolComponent<PoisonCloudBehavior>(
                "Poison Cloud", cloudPrefab, 10);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);
            if (_abilityCoroutine != null)
                StopCoroutine(_abilityCoroutine);
            _abilityCoroutine = StartCoroutine(AbilityLoop());
        }

        private IEnumerator AbilityLoop()
        {
            var lvl = AbilityLevel;
            // adjust these once
            float scaledCooldown = lvl.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;
            float spawnDelay     = lvl.TimeBetweenClouds * PlayerBehavior.Player.CooldownMultiplier;
            float totalSpawnTime = lvl.CloudsCount * spawnDelay;

            while (true)
            {
                // spawn your clouds
                for (int i = 0; i < lvl.CloudsCount; i++)
                {
                    var cloud = _pool.GetEntity();
                    Vector3 vp = new Vector3(Random.value, Random.value, Camera.main.nearClipPlane);
                    cloud.transform.position = Camera.main.ViewportToWorldPoint(vp);

                    cloud.Radius        = lvl.Radius;
                    cloud.Lifetime      = lvl.CloudLifetime;
                    cloud.Damage        = lvl.Damage;
                    cloud.TickInterval  = lvl.TickInterval;
                    cloud.SlowAmount    = lvl.SlowAmount;
                    cloud.SlowDuration  = lvl.SlowDuration;

                    cloud.Init();
                    yield return new WaitForSeconds(spawnDelay);
                }

                // wait out cooldown
                float remaining = scaledCooldown - totalSpawnTime;
                if (remaining < 0.1f) remaining = 0.1f;
                yield return new WaitForSeconds(remaining);
            }
        }

        public override void Clear()
        {
            if (_abilityCoroutine != null)
                StopCoroutine(_abilityCoroutine);
            _pool.Destroy();
            base.Clear();
        }
    }
}
