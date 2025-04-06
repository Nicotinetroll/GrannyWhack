using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class EnemyKingBehavior : EnemyBehavior
    {
        [SerializeField] Animator animator;

        [Header("Sword Bomb Attack")]
        [SerializeField] GameObject bombProjectilePrefab;
        [SerializeField] Transform bombSpawnPoint;
        [SerializeField] int shotsPerCast = 3;
        [SerializeField] float timeBetweenShots = 0.5f;
        [SerializeField] float bombDamageMultiplier = 1f;
        [SerializeField] private GameObject shootVFXPrefab;

        [Header("Enemy Spawning (Slime Style)")]
        [SerializeField] EnemyType spawnedEnemyType = EnemyType.Slime;
        [SerializeField] int spawningWavesCount = 3;
        [SerializeField] int spawnedEnemiesCount = 3;
        [SerializeField] float durationBetweenSpawning = 1f;
        [SerializeField] float durationBetweenWarningAndSpawning = 0.5f;
        [SerializeField] ParticleSystem spawningParticle;

        [Header("Timing")]
        [SerializeField] float moveDuration = 5f;
        [SerializeField] float shootCooldown = 1f;

        private PoolComponent<EnemyMegaSlimeProjectileBehavior> bombPool;
        private PoolComponent<ParticleSystem> shootVFXPool;
        private List<EnemyMegaSlimeProjectileBehavior> activeProjectiles = new();
        private List<WarningCircleBehavior> warningCircles = new();
        private bool isShooting;

        // 🔊 Audio hashes
        public static readonly int BOSSKING_SHOOT_HASH = "BossKing_Shoot".GetHashCode();
        public static readonly int BOSSKING_SPAWN_HASH = "BossKing_Spawn".GetHashCode();

        protected override void Awake()
        {
            base.Awake();

            bombPool = new PoolComponent<EnemyMegaSlimeProjectileBehavior>(bombProjectilePrefab, 6);
            shootVFXPool = new PoolComponent<ParticleSystem>(shootVFXPrefab, 6);
        }

        public override void Play()
        {
            base.Play();
            StartCoroutine(BehaviorCoroutine());
        }

        private IEnumerator BehaviorCoroutine()
        {
            while (true)
            {
                // 🟧 Walking phase
                IsMoving = true;
                animator.SetBool("IsShooting", false);
                yield return new WaitForSeconds(moveDuration);

                // 🔫 Shooting phase
                IsMoving = false;
                yield return StartCoroutine(ShootingRoutine());

                // 👾 Spawn enemies async
                StartCoroutine(SpawnEnemyWaves());

                yield return new WaitForSeconds(shootCooldown);
            }
        }

        private IEnumerator ShootingRoutine()
        {
            isShooting = true;
            animator.SetBool("IsShooting", true);

            for (int i = 0; i < shotsPerCast; i++)
            {
                animator.Play("King Casting Phase 2", 0, 0f);
                FireBomb();
                yield return new WaitForSeconds(timeBetweenShots);
            }

            animator.SetBool("IsShooting", false);
            isShooting = false;
        }

        private void FireBomb()
        {
            // 💣 Spawn projectile
            var sword = bombPool.GetEntity();
            sword.Init(bombSpawnPoint.position, Vector2.up);
            sword.Damage = StageController.Stage.EnemyDamage * bombDamageMultiplier;
            sword.onFinished += OnSwordFinished;
            activeProjectiles.Add(sword);

            // 💥 Play VFX
            var vfx = shootVFXPool.GetEntity();
            if (vfx != null)
            {
                vfx.transform.position = bombSpawnPoint.position;
                vfx.transform.rotation = Quaternion.identity;
                vfx.Play();
            }

            // 🔊 Play shoot sound
            GameController.AudioManager.PlaySound(BOSSKING_SHOOT_HASH);
        }

        private IEnumerator SpawnEnemyWaves()
        {
            for (int wave = 0; wave < spawningWavesCount; wave++)
            {
                GameController.AudioManager.PlaySound(BOSSKING_SPAWN_HASH);
                spawningParticle?.Play();

                for (int i = 0; i < spawnedEnemiesCount; i++)
                {
                    var pos = StageController.FieldManager.Fence.GetRandomPointInside(0.5f);
                    var warning = StageController.PoolsManager.GetEntity<WarningCircleBehavior>("Warning Circle");

                    if (warning == null) continue;

                    warning.transform.position = pos;
                    warning.Play(1f, 0.3f, 100, null);
                    warningCircles.Add(warning);
                }

                yield return new WaitForSeconds(durationBetweenWarningAndSpawning);

                foreach (var warning in warningCircles)
                {
                    StageController.EnemiesSpawner.Spawn(spawnedEnemyType, warning.transform.position);
                    warning.gameObject.SetActive(false);
                }

                warningCircles.Clear();
                spawningParticle?.Stop();

                yield return new WaitForSeconds(durationBetweenSpawning);
            }
        }

        private void OnSwordFinished(EnemyMegaSlimeProjectileBehavior sword)
        {
            sword.onFinished -= OnSwordFinished;
            activeProjectiles.Remove(sword);
        }

        protected override void Die(bool flash)
        {
            base.Die(flash);

            foreach (var sword in activeProjectiles)
            {
                sword.onFinished -= OnSwordFinished;
                sword.Clear();
            }

            activeProjectiles.Clear();

            foreach (var warning in warningCircles)
                warning.gameObject.SetActive(false);

            warningCircles.Clear();

            StopAllCoroutines();
        }
    }
}
