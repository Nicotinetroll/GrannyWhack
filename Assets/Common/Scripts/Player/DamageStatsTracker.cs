using UnityEngine;

public static class DamageStatsTracker
{
    public static float TotalDamage { get; private set; }
    public static float DPS => elapsedTime > 0 ? TotalDamage / elapsedTime : 0;

    private static float elapsedTime;

    public static void Reset()
    {
        TotalDamage = 0;
        elapsedTime = 0;
    }

    public static void AddDamage(float damage)
    {
        TotalDamage += damage;
    }

    public static void Update(float deltaTime)
    {
        elapsedTime += deltaTime;
    }
}