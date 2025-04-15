using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class BuffUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectMask2D mask;
        [SerializeField] private TMP_Text buffLabel;

        private float duration;
        private float timeRemaining;
        private bool isActive;

        public void ShowBuff(string buffName, float buffDuration)
        {
            if (string.IsNullOrEmpty(buffName) || buffDuration <= 0f)
                return;

            duration = buffDuration;
            timeRemaining = duration;

            if (buffLabel != null)
                buffLabel.text = buffName;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = false;
            }

            isActive = true;
        }

        public void Hide()
        {
            isActive = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            Destroy(gameObject); // ✅ Destroy this UI element when hidden
        }

        private void Update()
        {
            if (!isActive) return;

            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0f)
            {
                Hide(); // Automatically hides + destroys when done
                return;
            }

            float progress = Mathf.Clamp01(timeRemaining / duration);
            if (mask != null)
            {
                var padding = mask.padding;
                padding.z = mask.rectTransform.rect.width * (1 - progress);
                mask.padding = padding;
            }
        }
    }
}