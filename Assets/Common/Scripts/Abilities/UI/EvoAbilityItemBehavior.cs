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
            // 1) Enable all Image components in this clone
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                img.enabled = true;
            }

            // 2) assign the sprite & interactability
            iconImage.sprite      = iconSprite;
            _button.interactable   = unlocked;
            disabledOverlay.alpha = unlocked ? 0f : 1f;
        }
    }
}