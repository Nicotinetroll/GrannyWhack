// DeathBladeWeaponAbilityBehavior.cs
using System.Collections;
using System.Collections.Generic;
using OctoberStudio.Pool;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class DeathBladeWeaponAbilityBehavior 
        : AbilityBehavior<DeathBladeWeaponAbilityData, DeathBladeWeaponAbilityLevel>
    {
        public static readonly int DEATH_BLADE_ATTACK_HASH =
            "Death Blade Attack".GetHashCode();

        [SerializeField] private GameObject bladePrefab;

        private PoolComponent<DeathBladeSlashBehavior> _slashPool;
        private readonly List<DeathBladeSlashBehavior> _active = new();
        private Coroutine _abilityCoroutine;

        private float Cooldown =>
            AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;
        private float SlashDelay =>
            AbilityLevel.TimeBetweenSlashes * PlayerBehavior.Player.CooldownMultiplier;

        void Awake()
        {
            _slashPool = new PoolComponent<DeathBladeSlashBehavior>(
                "Death Blade Slash", bladePrefab, 50);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);
            if (_abilityCoroutine != null) Disable();
            _abilityCoroutine = StartCoroutine(AttackLoop());
        }

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                // 1) Find the single closest target for this entire volley
                EnemyBehavior closest = null;
                float bestSqr = float.MaxValue;
                Vector2 center = PlayerBehavior.CenterPosition;
                var enemies = StageController.EnemiesSpawner.GetAllEnemies();
                foreach (var e in enemies)
                {
                    float sqr = ((Vector2)e.transform.position - center).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        closest = e;
                    }
                }

                // 2) For each slash, spawn at that same target (or player if none)
                for (int i = 0; i < AbilityLevel.SlashesCount; i++)
                {
                    var slash = _slashPool.GetEntity();

                    if (closest != null)
                        slash.transform.position = closest.Center;
                    else
                        slash.transform.position = PlayerBehavior.CenterPosition;

                    // optional random Z rotation
                    if (AbilityLevel.RandomRotation)
                    {
                        float randomZ = Random.Range(0f, 360f);
                        slash.transform.rotation = Quaternion.Euler(0, 0, randomZ);
                    }

                    // configure slash
                    slash.DamageMultiplier = AbilityLevel.Damage;
                    slash.KickBack        = AbilityLevel.EnableKickBack;
                    slash.Size            = AbilityLevel.SlashSize;

                    slash.Init();
                    slash.onFinished += OnSlashFinished;
                    _active.Add(slash);

                    GameController.AudioManager.PlaySound(DEATH_BLADE_ATTACK_HASH);
                    yield return new WaitForSeconds(SlashDelay);
                }

                // 3) Cooldown until next volley
                float rem = Cooldown - AbilityLevel.SlashesCount * SlashDelay;
                if (rem < 0.1f) rem = 0.1f;
                yield return new WaitForSeconds(rem);
            }
        }

        private void OnSlashFinished(DeathBladeSlashBehavior slash)
        {
            slash.onFinished -= OnSlashFinished;
            _active.Remove(slash);
        }

        private void Disable()
        {
            foreach (var s in _active) s.Disable();
            _active.Clear();
            if (_abilityCoroutine != null)
                StopCoroutine(_abilityCoroutine);
        }

        public override void Clear()
        {
            Disable();
            base.Clear();
        }
    }
}
