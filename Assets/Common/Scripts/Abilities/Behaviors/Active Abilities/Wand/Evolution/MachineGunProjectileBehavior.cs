using UnityEngine;
using OctoberStudio;
using System.Collections.Generic;

public class MachineGunProjectileBehavior : SimplePlayerProjectileBehavior
{
    private int remainingBounces;
    private float bounceRadius;
    private int bouncesDone = 0;
    private float baseMultiplier;
    private readonly List<GameObject> alreadyHit = new();

    private const float damageFalloffPerBounce = 0.8f;

    [Header("Projectile Effect")]
    [SerializeField] private ParticleSystem projectileEffect;

    public void InitBounce(
        Vector2 position,
        Vector2 direction,
        float speed,
        float lifeTime,
        float damageMultiplier,
        int? bounceCount,
        float radius)
    {
        transform.position = position;
        transform.localScale = Vector3.one * PlayerBehavior.Player.SizeMultiplier;

        this.direction = direction.normalized;
        this.Speed = speed;
        this.LifeTime = lifeTime;
        this.bounceRadius = radius;

        if (bounceCount.HasValue)
        {
            this.remainingBounces = bounceCount.Value;
            this.bouncesDone = 0;
            this.baseMultiplier = damageMultiplier;
        }

        spawnTime = Time.time;
        alreadyHit.Clear();
        selfDestructOnHit = false;

        if (rotatingPart != null)
            rotatingPart.rotation = Quaternion.FromToRotation(Vector2.up, direction);

        if (trail != null)
            trail.Clear();

        foreach (var p in particles)
        {
            if (p == null) continue;
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.Clear();
            p.Play();
        }

        if (projectileEffect != null)
        {
            projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            projectileEffect.Clear();
            projectileEffect.Play();
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        transform.position += direction * Time.deltaTime * Speed;

        if (LifeTime > 0 && Time.time - spawnTime > LifeTime)
        {
            FinishProjectile();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy")) return;
        if (alreadyHit.Contains(collision.gameObject)) return;

        alreadyHit.Add(collision.gameObject);

        if (collision.TryGetComponent(out EnemyBehavior enemy))
        {
            float finalMultiplier = baseMultiplier * Mathf.Pow(damageFalloffPerBounce, bouncesDone);
            float finalDamage = PlayerBehavior.Player.Damage * finalMultiplier;

            enemy.TakeDamage(finalDamage);
        }

        if (remainingBounces > 0)
        {
            EnemyBehavior nextEnemy = FindNextTarget();
            if (nextEnemy != null)
            {
                remainingBounces--;
                bouncesDone++;

                direction = (nextEnemy.Center - (Vector2)transform.position).normalized;

                InitBounce(
                    transform.position,
                    direction,
                    Speed,
                    LifeTime,
                    baseMultiplier,
                    null,
                    bounceRadius
                );
                return;
            }
        }

        FinishProjectile();
    }

    private EnemyBehavior FindNextTarget()
    {
        float closestSqrDist = float.MaxValue;
        EnemyBehavior closestEnemy = null;

        var enemies = StageController.EnemiesSpawner.GetEnemiesInRadius(transform.position, bounceRadius);

        foreach (var enemy in enemies)
        {
            if (enemy == null || alreadyHit.Contains(enemy.gameObject)) continue;

            float sqrDist = (enemy.Center - (Vector2)transform.position).sqrMagnitude;

            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private void FinishProjectile()
    {
        if (projectileEffect != null)
        {
            projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        Clear();
        onFinished?.Invoke(this);
    }
}
