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
        public static readonly int DEATH_BLADE_ATTACK_HASH = "Death Blade Attack".GetHashCode();

        [SerializeField] private GameObject bladePrefab;

        private PoolComponent<DeathBladeSlashBehavior> _slashPool;
        private readonly List<DeathBladeSlashBehavior> _active = new();
        private Coroutine _abilityCoroutine;

        private float Cooldown => AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;
        private float SlashDelay => AbilityLevel.TimeBetweenSlashes * PlayerBehavior.Player.CooldownMultiplier;

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
                // find initial target (closest to player)
                var enemies = StageController.EnemiesSpawner.GetAllEnemies();
                Vector2 center = PlayerBehavior.CenterPosition;

                EnemyBehavior prev = null;
                float best = float.MaxValue;
                foreach (var e in enemies)
                {
                    float d = ((Vector2)e.transform.position - center).sqrMagnitude;
                    if (d < best) { best = d; prev = e; }
                }

                // chain through each slash
                for (int i = 0; i < AbilityLevel.SlashesCount; i++)
                {
                    var slash = _slashPool.GetEntity();
                    slash.transform.position = prev != null ? prev.Center : center;

                    if (AbilityLevel.RandomRotation)
                        slash.transform.rotation = Quaternion.Euler(0,0,Random.Range(0f,360f));

                    slash.DamageMultiplier = AbilityLevel.Damage;
                    slash.KickBack = AbilityLevel.EnableKickBack;
                    slash.Size = AbilityLevel.SlashSize;

                    slash.Init();
                    slash.onFinished += OnSlashFinished;
                    _active.Add(slash);

                    GameController.AudioManager.PlaySound(DEATH_BLADE_ATTACK_HASH);
                    yield return new WaitForSeconds(SlashDelay);

                    // decide bounce
                    if (prev != null && Random.value <= AbilityLevel.BounceChance)
                    {
                        EnemyBehavior next = null;
                        float minD = float.MaxValue;
                        Vector2 from = prev.Center;

                        // search within radius
                        foreach (var e in enemies)
                        {
                            if (e == prev) continue;
                            float d2 = ((Vector2)e.transform.position - from).sqrMagnitude;
                            if (d2 <= AbilityLevel.BounceRadius * AbilityLevel.BounceRadius && d2 < minD)
                            {
                                minD = d2; next = e;
                            }
                        }
                        prev = next ?? prev;
                    }
                    // else prev remains same
                }

                // cooldown
                float rem = Cooldown - AbilityLevel.SlashesCount * SlashDelay;
                if (rem < 0.1f) rem = 0.1f;
                yield return new WaitForSeconds(rem);
            }
        }

        private void OnSlashFinished(DeathBladeSlashBehavior s)
        {
            s.onFinished -= OnSlashFinished;
            _active.Remove(s);
        }

        private void Disable()
        {
            foreach (var s in _active) s.Disable();
            _active.Clear();
            if (_abilityCoroutine != null) StopCoroutine(_abilityCoroutine);
        }

        public override void Clear()
        {
            Disable();
            base.Clear();
        }
    }
}
