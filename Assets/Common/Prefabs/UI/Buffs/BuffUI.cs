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

        private void Start()
        {
            HideImmediate(); // ⬅ Make sure it’s hidden at start
        }

        public void ShowBuff(string buffName, float buffDuration)
        {
            if (string.IsNullOrEmpty(buffName) || buffDuration <= 0f)
                return;

            duration = buffDuration;
            timeRemaining = duration;
            buffLabel.text = buffName;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false;
            isActive = true;
        }

        public void Hide()
        {
            isActive = false;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void HideImmediate()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if (!isActive) return;

            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0f)
            {
                Hide();
                return;
            }

            float progress = Mathf.Clamp01(timeRemaining / duration);
            var padding = mask.padding;
            padding.z = mask.rectTransform.rect.width * (1 - progress);
            mask.padding = padding;
        }
    }
}