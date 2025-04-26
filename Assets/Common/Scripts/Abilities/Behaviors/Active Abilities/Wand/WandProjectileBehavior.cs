using UnityEngine;
using System.Collections.Generic;
using OctoberStudio;

public class WandProjectileBehavior : SimplePlayerProjectileBehavior
{
    private int remainingBounces;
    private float bounceRadius;
    private int bouncesDone;
    private float baseMultiplier;
    private List<GameObject> alreadyHit = new List<GameObject>();

    private const float DamageFalloffPerBounce = 0.8f;

    private float waveAmplitude = 1f;
    private float waveFrequency = 12f;

    private Vector2 waveAxisA;   // perpendicular axis
    private Vector2 waveAxisB;   // random second axis
    private float randomPhase;

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

        // 🌊 Setup double wave axes
        waveAxisA = new Vector2(-direction.y, direction.x).normalized;        // normal perpendicular
        waveAxisB = new Vector2(direction.y, direction.x).normalized;          // slanted offset
        randomPhase = Random.Range(0f, Mathf.PI * 2f);                         // random phase offset
    }

    private void Update()
    {
        if (spriteRenderer != null && !spriteRenderer.isVisible)
            Clear();

        float time = (Time.time - spawnTime);

        // basic forward movement
        transform.position += direction * Time.deltaTime * Speed;

        // 🎯 Add double wave offset
        Vector3 waveOffset =
            waveAxisA * Mathf.Sin(time * waveFrequency + randomPhase) * waveAmplitude * Time.deltaTime +
            waveAxisB * Mathf.Cos(time * waveFrequency * 0.7f + randomPhase) * waveAmplitude * 0.5f * Time.deltaTime;

        transform.position += waveOffset;

        if (LifeTime > 0f && time > LifeTime)
        {
            Clear();
            onFinished?.Invoke(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
            return;

        if (alreadyHit.Contains(collision.gameObject))
            return;

        alreadyHit.Add(collision.gameObject);

        if (collision.TryGetComponent(out EnemyBehavior enemy))
        {
            float finalDamage = PlayerBehavior.Player.Damage * baseMultiplier * Mathf.Pow(DamageFalloffPerBounce, bouncesDone);
            enemy.TakeDamage(finalDamage);
        }

        if (remainingBounces > 0)
        {
            var nextEnemy = FindNextTarget();
            if (nextEnemy != null)
            {
                remainingBounces--;
                bouncesDone++;
                direction = (nextEnemy.Center - (Vector2)transform.position).normalized;

                // 🔥 Recalculate wave directions after bounce
                waveAxisA = new Vector2(-direction.y, direction.x).normalized;
                waveAxisB = new Vector2(direction.y, direction.x).normalized;
                randomPhase = Random.Range(0f, Mathf.PI * 2f);

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
