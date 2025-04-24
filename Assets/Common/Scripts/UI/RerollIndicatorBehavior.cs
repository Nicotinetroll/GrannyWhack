using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OctoberStudio.Systems;

namespace OctoberStudio.UI
{
    public class RerollIndicatorBehavior : MonoBehaviour
    {
        [SerializeField] Image           icon;
        [SerializeField] TextMeshProUGUI amountText;

        void Start()
        {
            if (icon)
                icon.sprite = GameController.CurrenciesManager.GetIcon("Reroll");

            UpdateUI(RerollManager.Instance?.Stack ?? 0);
            RerollManager.OnStackChanged += UpdateUI;
        }

        void OnDestroy() => RerollManager.OnStackChanged -= UpdateUI;

        void UpdateUI(int value) => amountText.text = value.ToString();
    }
}