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
            slider.minValue     = 0f;
            slider.maxValue     = 1f;
            slider.wholeNumbers = false;
            slider.interactable = false;

            cfg = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            if (cfg == null)
                Debug.LogWarning("[CharacterXPBar] Missing CharacterLevelingConfig!");
        }

        /// <summary>Bind this bar to a character and show it.</summary>
        public void Setup(CharacterData character)
        {
            data           = character;
            gameObject.SetActive(true);
            UpdateBar();
        }

        void Update()
        {
            if (data != null && cfg != null)
                UpdateBar();
        }

        private void UpdateBar()
        {
            float totalXp    = CharacterLevelSystem.GetXp(data);
            int   lvl        = CharacterLevelSystem.GetLevel(data);

            // XP needed to reach this level
            float xpThis     = (lvl > 1)
                              ? cfg.GetXpForLevel(lvl)
                              : 0f;
            // XP needed to reach next level
            float xpNext     = (lvl < cfg.MaxLevel)
                              ? cfg.GetXpForLevel(lvl + 1)
                              : cfg.GetXpForLevel(lvl);

            float xpInto     = Mathf.Clamp(totalXp - xpThis, 0f, xpNext - xpThis);
            float xpSpan     = xpNext - xpThis;
            float frac       = (xpSpan > 0f)
                              ? xpInto / xpSpan
                              : 1f;

            slider.value = frac;

            if (xpValueText != null)
                xpValueText.text = $"{xpInto:F0}/{xpSpan:F0}";
            if (levelText != null)
                levelText.text   = lvl.ToString();
        }
    }
}
