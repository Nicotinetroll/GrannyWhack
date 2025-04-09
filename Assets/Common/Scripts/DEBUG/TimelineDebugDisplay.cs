using UnityEngine;
using UnityEngine.Playables;
using TMPro;
using System.Linq;
using OctoberStudio;

public class TimelineDebugDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private GameObject hitParticlePrefab;

    [Header("Settings")]
    [SerializeField] private float updateRate = 0.5f;

    private StageSave stageSave;
    private float timeElapsed;
    private int frameCount;

    private string versionText;

    private void Start()
    {
        versionText = $"v{Application.version}";

        stageSave = GameController.SaveManager.GetSave<StageSave>("Stage Save");

        RestoreDamageStats();      // ✅ Restore saved damage/DPS
        UpdateInitialDisplay();    // ✅ Show restored stats in UI
    }

    private void RestoreDamageStats()
    {
        if (stageSave == null) return;

        DamageStatsTracker.Restore(stageSave.TotalDamage, stageSave.DPS); // ✅ Uses safe method

        Debug.Log($"[Restore] Damage: {stageSave.TotalDamage}, DPS: {stageSave.DPS}");
    }

    private void UpdateInitialDisplay()
    {
        if (debugText != null)
        {
            debugText.text =
                $"[v{Application.version}]\n" +
                $"FPS: 0\n" +
                $"Hit Particles (Active): 0\n" +
                $"Total Damage: {DamageStatsTracker.TotalDamage:F0}\n" +
                $"DPS: {DamageStatsTracker.DPS:F1}";
        }
    }

    private void Update()
    {
        if (director == null || debugText == null || hitParticlePrefab == null) return;

        var allParticles = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        int totalParticles = allParticles.Sum(ps => ps.particleCount);

        int activeHit = 0;
        int disabledHit = 0;
        int unknownHit = 0;

        string hitName = hitParticlePrefab.name;

        foreach (var ps in allParticles)
        {
            var go = ps.gameObject;
            if (go.name.StartsWith(hitName))
            {
                if (go.activeInHierarchy)
                    activeHit++;
                else
                    disabledHit++;
            }
        }

        int totalFound = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Count(go => go.name.StartsWith(hitName));

        unknownHit = Mathf.Max(0, totalFound - (activeHit + disabledHit));

        frameCount++;
        timeElapsed += Time.unscaledDeltaTime;

        if (timeElapsed >= updateRate)
        {
            float fps = frameCount / timeElapsed;

            debugText.text =
                $"[v{Application.version}]\n" +
                $"FPS: {fps:F1}\n" +
                $"Hit Particles (Active): {activeHit}\n" +
                $"Total Damage: {DamageStatsTracker.TotalDamage:F0}\n" +
                $"DPS: {DamageStatsTracker.DPS:F1}";

            if (stageSave != null)
            {
                stageSave.TotalDamage = DamageStatsTracker.TotalDamage;
                stageSave.DPS = DamageStatsTracker.DPS;
            }

            frameCount = 0;
            timeElapsed = 0;
        }
        else if (!string.IsNullOrEmpty(debugText.text))
        {
            string[] lines = debugText.text.Split('\n');
            if (lines.Length >= 5)
            {
                lines[2] = $"Hit Particles (Active): {activeHit}";
                lines[3] = $"Total Damage: {DamageStatsTracker.TotalDamage:F0}";
                lines[4] = $"DPS: {DamageStatsTracker.DPS:F1}";
                debugText.text = string.Join("\n", lines);
            }
        }
    }
}
