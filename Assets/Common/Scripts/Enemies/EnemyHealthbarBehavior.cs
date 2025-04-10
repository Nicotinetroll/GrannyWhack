using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using UnityEngine;

namespace OctoberStudio.UI
{
    public class EnemyHealthbarBehavior : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer fillImage;
        [SerializeField] private SpriteRenderer backgroundImage;
        [SerializeField] private Transform maskTransform;
        [SerializeField] private float maskMaxPosition = 0.34f;
        [SerializeField] private float maskMaxScale = 0.132f;

        public float MaxHP { get; private set; }
        public float HP { get; private set; }

        public bool IsZero => HP <= 0f;
        public bool IsMax => Mathf.Approximately(HP, MaxHP);

        private bool autoShowOnChange = true;
        private bool autoHideWhenMax = false; // ✅ Always show bar by default

        private IEasingCoroutine showHideCoroutine;
        private bool isShown = false;

        public void Init(float maxHP)
        {
            MaxHP = Mathf.Max(maxHP, 1f);
            HP = MaxHP;
            Redraw();
            Show(); // ✅ Show immediately on Init
        }

        public void SetAutoShowOnChange(bool value) => autoShowOnChange = value;

        public void SetAutoHideWhenMax(bool value)
        {
            autoHideWhenMax = value;
            if (IsMax && autoHideWhenMax) ForceHide();
        }

        public void AddHP(float value)
        {
            if (value < 0f)
            {
                Subtract(-value);
                return;
            }

            HP += value;
            if (HP > MaxHP)
                HP = MaxHP;

            Redraw();
        }

        public void AddPercentage(float percent)
        {
            AddHP(MaxHP * percent / 100f);
        }

        public void Subtract(float value)
        {
            if (value < 0f)
            {
                AddHP(-value);
                return;
            }

            HP -= value;
            HP = Mathf.Max(0, HP);

            if (HP == 0)
            {
                Hide();
            }
            else
            {
                if (autoShowOnChange && !isShown)
                    Show();

                Redraw();
            }
        }

        public void ResetHP(float duration = 0f)
        {
            if (duration > 0f)
            {
                EasingManager.DoFloat(0f, MaxHP, duration, (hp) =>
                {
                    HP = hp;
                    Redraw();
                });

                Show();
            }
            else
            {
                HP = MaxHP;
                Redraw();
                Show();
            }
        }

        public void ChangeMaxHP(float newMaxHP, bool scaleHP = true)
        {
            newMaxHP = Mathf.Max(newMaxHP, 1f);
            float oldMaxHP = MaxHP;
            MaxHP = newMaxHP;

            if (scaleHP)
            {
                float scaleFactor = newMaxHP / oldMaxHP;
                HP *= scaleFactor;
                HP = Mathf.Clamp(HP, 0, MaxHP);
            }

            Redraw();
        }

        public void SetBarRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            HP = MaxHP * ratio;
            Redraw();
        }

        private void Redraw()
        {
            if (maskTransform == null) return;

            float t = MaxHP > 0 ? Mathf.Clamp01(HP / MaxHP) : 0f;
            maskTransform.localPosition = Vector3.left * maskMaxPosition * (1f - t);
            maskTransform.localScale = maskTransform.localScale.SetX(maskMaxScale * t);
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true); // ✅ Ensure it's not disabled in hierarchy

            isShown = true;
            Redraw();

            showHideCoroutine?.Stop();
            SetAlpha(1f); // ✅ Instantly visible
        }

        public void Hide()
        {
            isShown = false;

            showHideCoroutine?.Stop();
            showHideCoroutine = new FloatEasingCoroutine(fillImage.color.a, 0f, 0.3f, 0, SetAlpha)
                .SetEasing(EasingType.SineOut);
        }

        public void ForceHide()
        {
            isShown = false;
            SetAlpha(0f);
        }

        private void SetAlpha(float alpha)
        {
            if (fillImage != null) fillImage.SetAlpha(alpha);
            if (backgroundImage != null) backgroundImage.SetAlpha(alpha);
        }

        // ✅ Prevent flipping with enemy sprite
        private void LateUpdate()
        {
            // Prevent flipping when parent flips (e.g., negative scale.x)
            var parent = transform.parent;
            if (parent != null)
            {
                Vector3 localScale = transform.localScale;
                localScale.x = Mathf.Abs(localScale.x) * Mathf.Sign(parent.lossyScale.x); // Cancel out parent flip
                transform.localScale = localScale;
            }
        }

    }
}
