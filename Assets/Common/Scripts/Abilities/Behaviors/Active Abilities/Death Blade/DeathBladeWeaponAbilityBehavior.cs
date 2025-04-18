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
                // Gather live enemies
                var enemies = StageController.EnemiesSpawner.GetAllEnemies();
                Vector2 center = PlayerBehavior.CenterPosition;

                // Initial target: closest to player
                EnemyBehavior prevTarget = null;
                float bestDist = float.MaxValue;
                foreach (var e in enemies)
                {
                    float d = ((Vector2)e.transform.position - center).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        prevTarget = e;
                    }
                }

                // Chain through each slash as a bounce
                for (int i = 0; i < AbilityLevel.SlashesCount; i++)
                {
                    var slash = _slashPool.GetEntity();

                    if (prevTarget != null)
                        slash.transform.position = prevTarget.Center;
                    else
                        slash.transform.position = center;

                    // Random Z rotation if enabled
                    if (AbilityLevel.RandomRotation)
                    {
                        float rz = Random.Range(0f, 360f);
                        slash.transform.rotation = Quaternion.Euler(0, 0, rz);
                    }

                    // Configure slash
                    slash.DamageMultiplier = AbilityLevel.Damage;
                    slash.KickBack = AbilityLevel.EnableKickBack;
                    slash.Size = AbilityLevel.SlashSize;

                    slash.Init();
                    slash.onFinished += OnSlashFinished;
                    _active.Add(slash);

                    GameController.AudioManager.PlaySound(DEATH_BLADE_ATTACK_HASH);
                    yield return new WaitForSeconds(SlashDelay);

                    // Determine next bounce: closest to prevTarget (excluding itself)
                    EnemyBehavior next = null;
                    float minDist = float.MaxValue;
                    Vector2 fromPos = prevTarget != null ? prevTarget.Center : center;
                    foreach (var e in enemies)
                    {
                        if (e == prevTarget) continue;
                        float dist2 = ((Vector2)e.transform.position - fromPos).sqrMagnitude;
                        if (dist2 < minDist)
                        {
                            minDist = dist2;
                            next = e;
                        }
                    }
                    prevTarget = next ?? prevTarget;
                }

                // Cooldown
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
            foreach (var s in _active)
                s.Disable();
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
