using UnityEngine;
using UnityEngine.Playables;
using TMPro;
using System.Linq;
using OctoberStudio;
using OctoberStudio.UI;

public class TimelineDebugDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private GameObject hitParticlePrefab;
    [SerializeField] private PlayerStatsManager playerStats;

    [Header("Settings")]
    [SerializeField] private float updateRate = 0.5f;

    private StageSave stageSave;
    private float timeElapsed;
    private int frameCount;

    private void Start()
    {
        stageSave = GameController.SaveManager.GetSave<StageSave>("Stage Save");

        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStatsManager>();

        if (playerStats != null)
            playerStats.RestoreFromSave();

        UpdateInitialDisplay();
    }

    private void UpdateInitialDisplay()
    {
        if (debugText != null && playerStats != null)
        {
            debugText.text =
                $"[v{Application.version}]\n" +
                $"FPS: 0\n" +
                $"Hit Particles (Active): 0\n" +
                $"Total Damage: {playerStats.TotalDamage:F0}\n" +
                $"DPS: {playerStats.DPS:F1}";
        }
    }

    private void Update()
    {
        if (director == null || debugText == null || hitParticlePrefab == null || playerStats == null)
            return;

        var player = PlayerBehavior.Player;
        if (player == null) return;

        int activeHit = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
            .Count(p => p.gameObject.name.StartsWith(hitParticlePrefab.name) && p.gameObject.activeInHierarchy);

        frameCount++;
        timeElapsed += Time.unscaledDeltaTime;

        if (timeElapsed >= updateRate)
        {
            float fps = frameCount / timeElapsed;
            float magnetRange = Mathf.Sqrt(player.MagnetRadiusSqr);
            float currentHP = player.Healthbar.HP;
            float maxHP = player.Healthbar.MaxHP;


            debugText.text =
                $"[v{Application.version}]\n" +
                $"FPS: {fps:F1}\n" +
                $"Hit Particles (Active): {activeHit}\n" +
                $"Total Damage: {playerStats.TotalDamage:F0}\n" +
                $"DPS: {playerStats.DPS:F1}\n" +
                $"--- Player Stats ---\n" +
                $"Final Damage: {player.Damage:F1}\n" +
                $"HP: {currentHP:F0} / {maxHP:F0}\n" +
                $"Move Speed: {player.Speed:F2}\n" +
                $"Magnet Range: {magnetRange:F2}\n" +
                $"XP Multiplier: {player.XPMultiplier:F2}\n" +
                $"Cooldown Multiplier: {player.CooldownMultiplier:F2}\n" +
                $"Projectile Speed: {player.ProjectileSpeedMultiplier:F2}\n" +
                $"Projectile Size: {player.SizeMultiplier:F2}\n" +
                $"Duration Multiplier: {player.DurationMultiplier:F2}\n" +
                $"Gold Multiplier: {player.GoldMultiplier:F2}";

            stageSave.TotalDamage = playerStats.TotalDamage;
            stageSave.TimeAlive = playerStats.ElapsedTime;
            GameController.SaveManager.Save(true);

            frameCount = 0;
            timeElapsed = 0;
        }
    }
}
