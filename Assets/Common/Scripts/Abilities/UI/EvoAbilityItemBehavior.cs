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
            iconImage.sprite      = iconSprite;
            _button.interactable   = unlocked;
            disabledOverlay.alpha = unlocked ? 0f : 0.5f;
        }
    }
}