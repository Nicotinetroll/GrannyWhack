using UnityEngine;
using OctoberStudio;
using System.Collections.Generic;

public class MachineGunProjectileBehavior : SimplePlayerProjectileBehavior
{
    private int remainingBounces;
    private float bounceRadius;
    private int bouncesDone = 0;
    private float baseMultiplier;
    private List<GameObject> alreadyHit = new List<GameObject>();

    private const float damageFalloffPerBounce = 0.8f;

    [Header("VFX")]
    [SerializeField] private GameObject flashEffect;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private ParticleSystem projectileEffect;
    [SerializeField] private float hitOffset = 0.1f;

    public void InitBounce(
        Vector2 position,
        Vector2 direction,
        float speed,
        float lifeTime,
        float damageMultiplier,
        int? bounceCount,
        float radius
    )
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
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.Clear();
            p.Play();
        }

        // 🔥 Flash effect (play once and detach)
        if (flashEffect != null)
        {
            flashEffect.transform.SetParent(null);
            flashEffect.SetActive(true);

            if (flashEffect.TryGetComponent(out ParticleSystem flashPS))
            {
                flashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                flashPS.Play();
            }
        }

        // 🔁 Reset projectile effect
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

            // 💥 Hit effect (play and stay detached)
            if (hitEffect != null)
            {
                hitEffect.transform.position = transform.position + (Vector3)(direction * hitOffset);
                hitEffect.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
                hitEffect.SetActive(true);

                if (hitEffect.TryGetComponent(out ParticleSystem hitPS))
                {
                    hitPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    hitPS.Play();
                }
            }
        }

        if (remainingBounces > 0)
        {
            EnemyBehavior nextEnemy = FindNextTarget();
            if (nextEnemy != null)
            {
                remainingBounces--;
                bouncesDone++;

                direction = (nextEnemy.Center - (Vector2)transform.position).normalized;

                InitBounce(transform.position, direction, Speed, LifeTime, baseMultiplier, null, bounceRadius);
                return;
            }
        }

        FinishProjectile();
    }

    private EnemyBehavior FindNextTarget()
    {
        float closestDistance = float.MaxValue;
        EnemyBehavior nextEnemy = null;

        var allEnemies = StageController.EnemiesSpawner.GetEnemiesInRadius(transform.position, bounceRadius);

        foreach (var enemy in allEnemies)
        {
            if (enemy == null || alreadyHit.Contains(enemy.gameObject)) continue;

            float dist = (enemy.Center - (Vector2)transform.position).sqrMagnitude;
            if (dist < closestDistance)
            {
                closestDistance = dist;
                nextEnemy = enemy;
            }
        }

        return nextEnemy;
    }

    private void FinishProjectile()
    {
        // 💨 Stop projectile effect
        if (projectileEffect != null)
        {
            projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        Clear();
        onFinished?.Invoke(this);
    }
}
