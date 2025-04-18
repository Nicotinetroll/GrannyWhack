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
                for (int i = 0; i < AbilityLevel.SlashesCount; i++)
                {
                    // spawn slash
                    var slash = _slashPool.GetEntity();
                    slash.transform.position = PlayerBehavior.CenterPosition;

                    // pick target
                    var target =
                        StageController.EnemiesSpawner.GetClosestEnemy(PlayerBehavior.CenterPosition);
                    if (target != null)
                    {
                        // face from player to enemy
                        Vector2 dir = (target.Center - (Vector2)slash.transform.position).normalized;
                        slash.transform.rotation =
                            Quaternion.FromToRotation(Vector2.up, dir);
                        // move slash onto the enemy
                        slash.transform.position = target.Center;
                    }
                    else
                    {
                        // no enemies: random direction off‑screen
                        slash.transform.rotation = 
                            Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                    }

                    // configure and fire
                    slash.DamageMultiplier = AbilityLevel.Damage;
                    slash.KickBack = false;
                    slash.Size = AbilityLevel.SlashSize;
                    slash.Init();
                    slash.onFinished += OnSlashFinished;
                    _active.Add(slash);

                    GameController.AudioManager.PlaySound(DEATH_BLADE_ATTACK_HASH);
                    yield return new WaitForSeconds(SlashDelay);
                }

                // wait remaining cooldown
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
