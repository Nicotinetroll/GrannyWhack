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

        [Header("Padding Bounds (px)")]
        [Tooltip("Right padding when XP = 0% (completely empty)")]
        [SerializeField] private float emptyPadding = 528.7f;
        [Tooltip("Right padding when XP = 100% (completely full)")]
        [SerializeField] private float fullPadding  = 11.6f;

        private CharacterData data;
        private CharacterLevelingConfig cfg;
        private CanvasGroup canvasGroup;

        void Awake()
        {
            // grab the leveling config used by CharacterLevelSystem
            cfg = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;  // hide until Setup is called
        }

        /// <summary>
        /// Binds this bar to a character and makes it visible.
        /// </summary>
        public void Setup(CharacterData character)
        {
            data = character;
            canvasGroup.alpha = 1f;
            UpdateFill();
        }

        void Update()
        {
            if (data != null && cfg != null)
                UpdateFill();
        }

        private void UpdateFill()
        {
            // 1) get total XP and current level
            float totalXp = CharacterLevelSystem.GetXp(data);
            int   lvl     = CharacterLevelSystem.GetLevel(data);

            // 2) compute XP needed for current & next level
            //    (fixed call: no stray <user__selection> tags)
            float xpThis  = (lvl > 1)
                            ? cfg.GetXpForLevel(lvl)
                            : 0f;
            float xpNext  = cfg.GetXpForLevel(Mathf.Min(lvl + 1, cfg.MaxLevel));

            // 3) fraction [0,1] of XP through this level
            float inLevel = Mathf.Clamp(totalXp - xpThis, 0f, xpNext - xpThis);
            float frac    = (xpNext - xpThis) > 0f
                            ? inLevel / (xpNext - xpThis)
                            : 1f;

            // 4) linearly interpolate right‐padding between emptyPadding → fullPadding
            var pad      = rectMask.padding;
            pad.z        = Mathf.Lerp(emptyPadding, fullPadding, frac);
            rectMask.padding = pad;
        }
    }
}