using UnityEngine;
using System.Collections.Generic;
using OctoberStudio;

public class WandProjectileBehavior : SimplePlayerProjectileBehavior
{
    private int remainingBounces;
    private float bounceRadius;
    private int bouncesDone;
    private float baseMultiplier;
    private List<GameObject> alreadyHit = new();

    private float waveAmplitude = 1f;   // ← how far left/right
    private float waveFrequency = 12f;  // ← how fast oscillates
    private float waveTimer;

    private const float DamageFalloffPerBounce = 0.8f;

    public void InitBounce(
        Vector2 position,
        Vector2 direction,
        float speed,
        float lifeTime,
        float damageMultiplier,
        int? bounceCount,
        float radius)
    {
        base.Init(position, direction);

        transform.localScale = Vector3.one * PlayerBehavior.Player.SizeMultiplier;

        Speed = speed;
        LifeTime = lifeTime;
        baseMultiplier = damageMultiplier;
        bounceRadius = radius;

        remainingBounces = bounceCount ?? 0;
        bouncesDone = 0;
        alreadyHit.Clear();
        selfDestructOnHit = false;

        waveTimer = 0f; // reset wave
    }

    private void Update()
    {
        waveTimer += Time.deltaTime * waveFrequency;

        Vector3 waveOffset = transform.right * Mathf.Sin(waveTimer) * waveAmplitude;
        transform.position += (direction * Speed * Time.deltaTime) + (waveOffset * Time.deltaTime);

        if (LifeTime > 0 && Time.time - spawnTime > LifeTime)
        {
            Clear();
            onFinished?.Invoke(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.TryGetComponent<EnemyBehavior>(out var enemy)) return;
        if (alreadyHit.Contains(enemy.gameObject)) return;

        alreadyHit.Add(enemy.gameObject);

        float finalDamage = PlayerBehavior.Player.Damage * baseMultiplier * Mathf.Pow(DamageFalloffPerBounce, bouncesDone);
        enemy.TakeDamage(finalDamage);

        if (remainingBounces > 0)
        {
            var nextEnemy = FindNextTarget();
            if (nextEnemy != null)
            {
                remainingBounces--;
                bouncesDone++;
                direction = (nextEnemy.Center - (Vector2)transform.position).normalized;
                return;
            }
        }

        Clear();
        onFinished?.Invoke(this);
    }

    private EnemyBehavior FindNextTarget()
    {
        var enemies = StageController.EnemiesSpawner.GetEnemiesInRadius(transform.position, bounceRadius);
        EnemyBehavior closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || alreadyHit.Contains(enemy.gameObject)) continue;

            float dist = (enemy.Center - (Vector2)transform.position).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }
}
