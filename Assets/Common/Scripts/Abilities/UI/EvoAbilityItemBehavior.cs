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
            // Cache the Button (will also cover null-case)
            _button = GetComponent<Button>();
            if (_button == null)
                Debug.LogWarning("[EvoAbilityItemBehavior] No Button found on " + name);
        }

        /// <summary>
        /// iconSprite: the ability’s icon.
        /// unlocked:   whether the player’s level >= the required Evo level.
        /// </summary>
        public void Setup(Sprite iconSprite, bool unlocked)
        {
            // 1) assign the sprite if possible
            if (iconImage != null)
                iconImage.sprite = iconSprite;
            else
                Debug.LogWarning($"[EvoAbilityItemBehavior] iconImage is null on '{name}'");

            // 2) button interactability
            if (_button != null)
                _button.interactable = unlocked;

            // 3) show/hide the overlay
            if (disabledOverlay != null)
            {
                disabledOverlay.alpha          = unlocked ? 0f : 1f;
                disabledOverlay.blocksRaycasts = !unlocked;
            }
        }
    }
}