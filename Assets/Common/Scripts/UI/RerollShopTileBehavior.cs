using OctoberStudio.Audio;
using OctoberStudio.Currency;
using OctoberStudio.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.Upgrades.UI
{
    /// <summary>Slot inside *Upgrades Window* that lets player buy unlimited re-rolls.</summary>
    public class RerollShopTileBehavior : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] Image    iconImage;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text amountLabel;          // shows xN
        [SerializeField] Button   buyButton;
        [SerializeField] TMP_Text priceLabel;
        [SerializeField] Sprite   enabledBtnSprite;
        [SerializeField] Sprite   disabledBtnSprite;

        CurrencySave goldSave;

        /* ---------- init ---------- */
        void Start()
        {
            goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
            goldSave.onGoldAmountChanged += OnGoldChanged;

            iconImage.sprite = GameController.CurrenciesManager.GetIcon("Reroll");
            titleLabel.text  = "REROLL";

            buyButton.onClick.AddListener(OnBuy);

            RerollManager.OnStackChanged += UpdateVisuals;
            UpdateVisuals(RerollManager.Instance.Stack);
        }

        void OnDestroy()
        {
            goldSave.onGoldAmountChanged   -= OnGoldChanged;
            RerollManager.OnStackChanged   -= UpdateVisuals;
            buyButton.onClick.RemoveListener(OnBuy);
        }

        /* ---------- callbacks ---------- */
        void OnBuy()
        {
            bool ok = RerollManager.Instance.TryBuyInMenu();

            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            /* keep focus on the button for game-pad navigation */
            EventSystem.current.SetSelectedGameObject(buyButton.gameObject);

            if (ok) UpdateVisuals(RerollManager.Instance.Stack);
        }

        void OnGoldChanged(int _) => RedrawButton();

        /* ---------- UI refresh ---------- */
        void UpdateVisuals(int stack)
        {
            amountLabel.text = $"x{stack}";
            priceLabel.text  = $"<sprite name=\"Gold\"> {RerollManager.Instance.MenuPrice}";
            RedrawButton();
        }

        void RedrawButton()
        {
            bool canAfford = goldSave.CanAfford(RerollManager.Instance.MenuPrice);

            buyButton.interactable  = canAfford;
            buyButton.image.sprite  = canAfford ? enabledBtnSprite : disabledBtnSprite;
        }
    }
}
