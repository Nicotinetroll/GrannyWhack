using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(Slider))]
    public class CharacterXPBar : MonoBehaviour
    {
        [SerializeField] private Slider        slider;
        [SerializeField] private TextMeshProUGUI xpValueText;
        [SerializeField] private TMP_Text       levelText;

        CharacterData data;
        CharacterLevelingConfig cfg;

        void Awake()
        {
            if (slider == null) slider = GetComponent<Slider>();
            slider.minValue     = 0f;
            slider.maxValue     = 1f;
            slider.interactable = false;

            cfg = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            if (cfg == null)
                Debug.LogWarning("[CharacterXPBar] Missing CharacterLevelingConfig.");
        }

        public void Setup(CharacterData character)
        {
            data = character;
            gameObject.SetActive(true);
            UpdateBar();
        }

        void Update()
        {
            if (data != null && cfg != null)
                UpdateBar();
        }

        void UpdateBar()
        {
            float totalXp = CharacterLevelSystem.GetXp(data);
            int   lvl     = CharacterLevelSystem.GetLevel(data);

            float xpThis = cfg.GetCumulativeXpForLevel(lvl);
            float xpNext = cfg.GetCumulativeXpForLevel(
                Mathf.Min(lvl + 1, cfg.MaxLevel));

            float span  = xpNext - xpThis;
            float into  = Mathf.Clamp(totalXp - xpThis, 0f, span);
            float frac  = (span > 0f) ? (into / span) : 1f;

            slider.value = frac;

            if (xpValueText != null)
                xpValueText.text = $"{into:F0}/{span:F0}";
            if (levelText != null)
                levelText.text   = lvl.ToString();
        }
    }
}