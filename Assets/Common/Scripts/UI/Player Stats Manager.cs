using TMPro;
using UnityEngine;
using OctoberStudio;

namespace OctoberStudio.UI
{
    public class PlayerStatsManager : MonoBehaviour
    {
        public static PlayerStatsManager Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text dpsText;

        private StageSave stageSave;
        
        public float ElapsedTime => elapsedTime;


        private float elapsedTime = 0f;
        public float TotalDamage { get; private set; }
        public float DPS => elapsedTime > 0f ? TotalDamage / elapsedTime : 0f;

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
            TotalDamage = 0;
            elapsedTime = 0;

            stageSave.TotalDamage = 0;
            stageSave.TimeAlive = 0;
            GameController.SaveManager.Save(true);

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (damageText != null)
                damageText.text = $"DMG: {TotalDamage:F0}";

            if (dpsText != null)
                dpsText.text = $"DPS: {DPS:F1}";
        }
        
        public void RestoreFromSave()
        {
            TotalDamage = stageSave.TotalDamage;
            elapsedTime = stageSave.TimeAlive;
        }
    }
}