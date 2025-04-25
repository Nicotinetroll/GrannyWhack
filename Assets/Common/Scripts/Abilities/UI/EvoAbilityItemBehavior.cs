using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(Button))]
    public class EvoAbilityItemBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image       iconImage;
        [SerializeField] private CanvasGroup disabledOverlay;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button == null)
                Debug.LogWarning("[EvoAbilityItemBehavior] No Button on " + name);
        }

        /// <summary>
        /// iconSprite: the ability’s icon.
        /// unlocked:   whether the player’s level >= the required Evo level.
        /// </summary>
        public void Setup(Sprite iconSprite, bool unlocked)
        {
            // 1) icon
            if (iconImage != null)
            {
                if (iconSprite != null)
                {
                    iconImage.sprite = iconSprite;
                    iconImage.enabled = unlocked;
                }
                else
                {
                    iconImage.enabled = false;
                    Debug.LogWarning($"[EvoAbilityItem] '{name}' received null sprite!");
                }
            }

            // 2) interactability
            if (_button != null)
                _button.interactable = unlocked;

            // 3) overlay
            if (disabledOverlay != null)
            {
                disabledOverlay.alpha = unlocked ? 0f : 1f;
                disabledOverlay.blocksRaycasts = !unlocked;
            }

            Debug.Log($"[EvoAbilityItem] '{name}' unlocked={unlocked}");
        }

    }
}