using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OctoberStudio;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(Slider))]
    public class CharacterXPBar : MonoBehaviour
    {
        [Header("Required UI References")]
        [SerializeField] private Slider slider;
        [Tooltip("Optional: shows current XP / needed XP")]
        [SerializeField] private TextMeshProUGUI xpValueText;
        [Tooltip("Optional: shows the numeric level over the bar")]
        [SerializeField] private TMP_Text levelText;

        private CharacterData data;
        private CharacterLevelingConfig cfg;

        void Awake()
        {
            if (slider == null) slider = GetComponent<Slider>();
            slider.interactable = false; // read‑only

            cfg = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            if (cfg == null)
                Debug.LogWarning("[CharacterXPBar] Could not find CharacterLevelingConfig in Resources!");
        }

        /// <summary>
        /// Bind this bar to a character and make it visible.
        /// </summary>
        public void Setup(CharacterData character)
        {
            data = character;
            gameObject.SetActive(true);
            UpdateBar();
        }

        void Update()
        {
            // keep it live if XP/level changes at runtime
            if (data != null && cfg != null)
                UpdateBar();
        }

        private void UpdateBar()
        {
            float totalXp = CharacterLevelSystem.GetXp(data);
            int   lvl     = CharacterLevelSystem.GetLevel(data);

            // XP required to start this level
            float xpThis = (lvl > 1) 
                ? cfg.GetXpForLevel(lvl) 
                : 0f;

            // XP threshold for next level
            float xpNext = cfg.GetXpForLevel(
                Mathf.Min(lvl + 1, cfg.MaxLevel));

            // fraction [0..1] of progress through current level
            float inLevel = Mathf.Clamp(totalXp - xpThis, 0f, xpNext - xpThis);
            float frac = (xpNext - xpThis) > 0f
                ? inLevel / (xpNext - xpThis)
                : 1f;

            slider.value = frac;

            if (xpValueText != null)
                xpValueText.text = $"{inLevel:F0}/{xpNext - xpThis:F0}";
            if (levelText != null)
                levelText.text = lvl.ToString();
        }
    }
}
