using System.Collections;
using UnityEngine;
using OctoberStudio.Pool;

namespace OctoberStudio.Abilities
{
    public class PoisonCloudAbilityBehavior 
        : AbilityBehavior<PoisonCloudAbilityData, PoisonCloudAbilityLevel>
    {
        public static readonly int POISON_CLOUD_SPAWN_HASH =
            "Poison Cloud Spawn".GetHashCode();

        [Header("Prefab")]
        [SerializeField] private GameObject cloudEffectPrefab;

        private PoolComponent<PoisonCloudBehavior> _pool;
        private Coroutine _abilityCoroutine;

        private void Awake()
        {
            _pool = new PoolComponent<PoisonCloudBehavior>(
                "Poison Cloud", cloudEffectPrefab, 10);
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
            var lvl          = AbilityLevel;
            float scaledCD   = lvl.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;
            float spawnDelay = lvl.TimeBetweenClouds * PlayerBehavior.Player.CooldownMultiplier;
            float totalSpawn = lvl.CloudsCount * spawnDelay;

            while (true)
            {
                for (int i = 0; i < lvl.CloudsCount; i++)
                {
                    // pick a random point on screen
                    Vector3 vp   = new Vector3(Random.value, Random.value, Camera.main.nearClipPlane);
                    Vector3 pos  = Camera.main.ViewportToWorldPoint(vp);

                    // spawn & configure the cloud
                    var cloud = _pool.GetEntity();
                    cloud.transform.position = pos;
                    cloud.Radius       = lvl.Radius;
                    cloud.Lifetime     = lvl.CloudLifetime;
                    cloud.Damage       = lvl.Damage;
                    cloud.TickInterval = lvl.TickInterval;
                    cloud.SlowAmount   = lvl.SlowAmount;
                    cloud.SlowDuration = lvl.SlowDuration;
                    cloud.Init();  // ← no parameters

                    // play spawn sound
                    GameController.AudioManager.PlaySound(POISON_CLOUD_SPAWN_HASH);

                    yield return new WaitForSeconds(spawnDelay);
                }

                float remaining = scaledCD - totalSpawn;
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
