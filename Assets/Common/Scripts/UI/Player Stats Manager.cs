using TMPro;
using UnityEngine;
using OctoberStudio;

namespace OctoberStudio.UI
{
    public class PlayerStatsManager : MonoBehaviour
    {
        public static PlayerStatsManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text dpsText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text projectileSpeedText;

        private StageSave stageSave;
        private float elapsedTime = 0f;

        public float TotalDamage { get; private set; }
        public float DPS => elapsedTime > 0f ? TotalDamage / elapsedTime : 0f;
        public float ElapsedTime => elapsedTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");

            RestoreFromSave();
        }

        private void Update()
        {
            elapsedTime += Time.unscaledDeltaTime;

            stageSave.TotalDamage = TotalDamage;
            stageSave.TimeAlive = elapsedTime;

            UpdateUI();
        }

        public void AddDamage(float amount)
        {
            TotalDamage += amount;
        }

        public void ResetStats()
        {
            TotalDamage = 0f;
            elapsedTime = 0f;

            stageSave.TotalDamage = 0f;
            stageSave.TimeAlive = 0f;
            GameController.SaveManager.Save(true);

            UpdateUI();
        }

        public void RestoreFromSave()
        {
            TotalDamage = stageSave.TotalDamage;
            elapsedTime = stageSave.TimeAlive;
        }

        private void UpdateUI()
        {
            var player = PlayerBehavior.Player;
            if (player == null) return;

            if (damageText != null)
                damageText.text = $"DMG: {TotalDamage:F0}";

            if (dpsText != null)
                dpsText.text = $"DPS: {DPS:F1}";

            if (hpText != null && player.TryGetComponent(out HealthbarBehavior health))
                hpText.text = $"HP: {health.HP:F0} / {health.MaxHP:F0}";

            if (speedText != null)
                speedText.text = $"Move Speed: {player.Speed:F1}";

            if (cooldownText != null)
                cooldownText.text = $"Cooldown Mult: {player.CooldownMultiplier:F2}";

            if (projectileSpeedText != null)
                projectileSpeedText.text = $"Projectile Speed: {player.ProjectileSpeedMultiplier:F2}";
        }
    }
}
