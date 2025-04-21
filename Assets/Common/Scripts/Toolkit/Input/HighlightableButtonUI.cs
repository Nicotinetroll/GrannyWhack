using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(Button))]
    public class HighlightableButtonUI : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public bool IsHighlighted { get; set; }
        protected Button button;
        private Vector3 savedPosition = Vector3.zero;

        protected virtual void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Update()
        {
            if (!IsHighlighted) return;

            // if the UI element moved (e.g. scroll), refresh the highlight overlay
            if (transform.position != savedPosition)
            {
                savedPosition = transform.position;

                var im = GameController.InputManager;
                if (im?.Highlights != null)
                {
                    im.Highlights.RefreshHighlight();
                }
                else
                {
                    Debug.LogWarning($"[{name}] Cannot RefreshHighlight – " +
                        $"{(im == null ? "InputManager is null" : "Highlights is null")}");
                }
            }
        }

        public virtual void Highlight()
        {
            if (!button.enabled) return;

            var im = GameController.InputManager;
            if (im == null)
            {
                Debug.LogWarning($"[{name}] Cannot Highlight – GameController.InputManager is null.");
                return;
            }

            var hl = im.Highlights;
            if (hl == null)
            {
                Debug.LogWarning($"[{name}] Cannot Highlight – InputManager.Highlights is null.");
                return;
            }

            hl.Highlight(this);
            savedPosition = transform.position;
        }

        public virtual void StopHighlighting()
        {
            if (!IsHighlighted) return;

            var im = GameController.InputManager;
            if (im?.Highlights != null)
            {
                im.Highlights.StopHighlighting(this);
            }
            else
            {
                Debug.LogWarning($"[{name}] Cannot StopHighlighting – " +
                    $"{(im == null ? "InputManager is null" : "Highlights is null")}");
            }
        }

        private void OnDisable()
        {
            if (IsHighlighted)
                StopHighlighting();
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            Highlight();
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            StopHighlighting();
        }
    }
}
