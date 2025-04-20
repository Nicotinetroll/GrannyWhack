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
        }

        /// <summary>
        /// iconSprite: the ability’s icon.
        /// unlocked:   whether the player’s level >= the required Evo level.
        /// </summary>
        public void Setup(Sprite iconSprite, bool unlocked)
        {
            // 1) assign the sprite
            iconImage.sprite = iconSprite;

            // 2) button interactability
            _button.interactable = unlocked;

            // 3) show/hide the overlay
            if (disabledOverlay != null)
            {
                // fully transparent when unlocked, opaque when locked
                disabledOverlay.alpha = unlocked ? 0f : 1f;
                // block clicks when locked
                disabledOverlay.blocksRaycasts = !unlocked;
            }
        }
    }
}