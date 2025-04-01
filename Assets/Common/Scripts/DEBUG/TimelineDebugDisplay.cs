using UnityEngine;
using UnityEngine.Playables;
using TMPro;
using System.Linq;

public class TimelineDebugDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private GameObject hitParticlePrefab;

    [Header("Settings")]
    [SerializeField] private float updateRate = 0.5f;

    private float timeElapsed;
    private int frameCount;

    private void Update()
    {
        if (director == null || debugText == null || hitParticlePrefab == null) return;

        double timelineTime = director.time;
        int timelineFrame = Mathf.FloorToInt((float)(timelineTime * 60f));

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
                //$"Time: {timelineTime:F2}s\n" +
                //$"Frame: {timelineFrame}\n" +
                $"FPS: {fps:F1}\n" +
                //$"Particles: {totalParticles}\n" +
                $"Hit Particles (Active): {activeHit}\n" +
                //$"Hit Particles (Pooled/Disabled): {disabledHit}\n" +
                //$"Hit Particles (Missing): {unknownHit}\n" +
                $"Total Damage: {DamageStatsTracker.TotalDamage:F0}\n" +
                $"DPS: {DamageStatsTracker.DPS:F1}";

            frameCount = 0;
            timeElapsed = 0;
        }
        else if (!string.IsNullOrEmpty(debugText.text))
        {
            string[] lines = debugText.text.Split('\n');
            if (lines.Length >= 9)
            {
                lines[3] = $"Particles: {totalParticles}";
                lines[4] = $"Hit Particles (Active): {activeHit}";
                lines[5] = $"Hit Particles (Pooled/Disabled): {disabledHit}";
                lines[6] = $"Hit Particles (Missing): {unknownHit}";
                lines[7] = $"Damage: {DamageStatsTracker.TotalDamage:F0}";
                lines[8] = $"DPS: {DamageStatsTracker.DPS:F1}";
                debugText.text = string.Join("\n", lines);
            }
        }
    }
}
