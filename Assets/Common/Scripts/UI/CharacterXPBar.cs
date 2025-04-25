using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(Slider))]
    public class CharacterXPBar : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] Slider        slider;
        [SerializeField] TMP_Text      xpValueText;
        [SerializeField] TMP_Text      levelText;

        /* runtime refs */
        CharacterData              data;
        CharacterLevelingConfig    cfg;

        /* ───────────────────────── Awake ───────────────────────── */
        void Awake()
        {
            if (!slider) slider = GetComponent<Slider>();

            slider.minValue     = 0f;
            slider.maxValue     = 1f;
            slider.interactable = false;

            LoadConfig();          // try once in Awake
        }

        void LoadConfig()
        {
            if (cfg != null) return;

            cfg = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            if (cfg == null)
                Debug.LogWarning("[CharacterXPBar] CharacterLevelingConfig asset " +
                                 "not found in a Resources folder.");
        }

        /* ─────────────────────── public API ─────────────────────── */
        public void Setup(CharacterData character)
        {
            data = character;
            gameObject.SetActive(true);

            LoadConfig();          // ensure cfg exists
            UpdateBar(true);       // force an init draw
        }

        /* ─────────────────────── Update loop ────────────────────── */
        void Update()
        {
            UpdateBar(false);
        }

        /* ─────────────────────── internal ───────────────────────── */
        void UpdateBar(bool forceDraw)
        {
            /* no data or config yet → skip this frame */
            if (data == null || cfg == null) return;

            /* avoid needless work while slider isn’t visible */
            if (!forceDraw && !slider.gameObject.activeInHierarchy) return;

            float totalXp = CharacterLevelSystem.GetXp(data);
            int   lvl     = CharacterLevelSystem.GetLevel(data);

            float xpThis = cfg.GetCumulativeXpForLevel(lvl);
            float xpNext = cfg.GetCumulativeXpForLevel(
                               Mathf.Min(lvl + 1, cfg.MaxLevel));

            float span  = xpNext - xpThis;
            float into  = Mathf.Clamp(totalXp - xpThis, 0f, span);
            float frac  = (span > 0f) ? (into / span) : 1f;

            slider.value = frac;

            if (xpValueText)
                xpValueText.text = $"{into:F0}/{span:F0}";
            if (levelText)
                levelText.text   = lvl.ToString();
        }
    }
}
