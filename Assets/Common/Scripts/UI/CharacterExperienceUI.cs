using OctoberStudio.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CharacterExperienceUI : MonoBehaviour
    {
        [Header("XP Fill Mask")]
        [SerializeField] private RectMask2D rectMask;

        private CharacterData data;
        private CharacterLevelingConfig cfg;

        void Awake()
        {
            // Load the same config your level system uses
            cfg = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            // Hide until you call Setup
            GetComponent<CanvasGroup>().alpha = 0f;
        }

        /// <summary>
        /// Call once to bind this bar to a specific character.
        /// </summary>
        public void Setup(CharacterData character)
        {
            data = character;
            GetComponent<CanvasGroup>().alpha = 1f;
            UpdateFill();
        }

        void Update()
        {
            if (data != null && cfg != null)
                UpdateFill();
        }

        private void UpdateFill()
        {
            // total XP & current level
            float totalXp = CharacterLevelSystem.GetXp(data);
            int   lvl     = CharacterLevelSystem.GetLevel(data);

            // XP thresholds
            float xpForThis = (lvl > 1) ? cfg.GetXpForLevel(lvl) : 0f;
            float xpForNext = cfg.GetXpForLevel(Mathf.Min(lvl + 1, cfg.MaxLevel));

            // normalized progress in [0,1]
            float xpInLevel = Mathf.Clamp(totalXp - xpForThis, 0f, xpForNext - xpForThis);
            float frac      = (xpForNext - xpForThis) > 0f
                              ? xpInLevel / (xpForNext - xpForThis)
                              : 1f;

            // adjust right‑padding to mask off the empty portion
            float width = rectMask.rectTransform.rect.width;
            var pad     = rectMask.padding;
            pad.z       = width * (1f - frac);
            rectMask.padding = pad;
        }
    }
}
