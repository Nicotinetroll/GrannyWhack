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

        [Header("Enemy Spawning")]
        [SerializeField] EnemyType spawnedEnemyType = EnemyType.Slime;
        [SerializeField] int spawningWavesCount = 3;
        [SerializeField] int spawnedEnemiesCount = 3;
        [SerializeField] float durationBetweenSpawning = 1f;
        [SerializeField] float durationBetweenWarningAndSpawning = 0.5f;
        [SerializeField] ParticleSystem spawningParticle;

        [Header("Timing")]
        [SerializeField] float moveDuration = 5f;

        private PoolComponent<EnemyMegaSlimeProjectileBehavior> bombPool;
        private List<EnemyMegaSlimeProjectileBehavior> activeProjectiles = new();
        private List<WarningCircleBehavior> warningCircles = new();
        private bool isShooting;

        protected override void Awake()
        {
            base.Awake();
            bombPool = new PoolComponent<EnemyMegaSlimeProjectileBehavior>(bombProjectilePrefab, 6);
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
                // 🔶 WALK
                IsMoving = true;
                animator.SetBool("IsShooting", false);
                yield return new WaitForSeconds(moveDuration);

                // 🔷 SHOOT instantly
                IsMoving = false;
                yield return StartCoroutine(ShootingRoutine());

                // ✅ Start spawning in background (non-blocking)
                StartCoroutine(SpawnEnemyWaves());

                // 🔶 Go back to walking immediately
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
            var sword = bombPool.GetEntity();
            sword.Init(bombSpawnPoint.position, Vector2.up);
            sword.Damage = StageController.Stage.EnemyDamage * bombDamageMultiplier;
            sword.onFinished += OnSwordFinished;
            activeProjectiles.Add(sword);
        }

        private void OnSwordFinished(EnemyMegaSlimeProjectileBehavior sword)
        {
            sword.onFinished -= OnSwordFinished;
            activeProjectiles.Remove(sword);
        }

        private IEnumerator SpawnEnemyWaves()
        {
            for (int wave = 0; wave < spawningWavesCount; wave++)
            {
                spawningParticle?.Play();

                for (int i = 0; i < spawnedEnemiesCount; i++)
                {
                    var pos = StageController.FieldManager.Fence.GetRandomPointInside(0.5f);
                    var warning = StageController.PoolsManager.GetEntity<WarningCircleBehavior>("Warning Circle");

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
