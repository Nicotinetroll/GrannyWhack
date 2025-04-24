using OctoberStudio.Audio;
using OctoberStudio.Currency;
using OctoberStudio.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.Upgrades.UI
{
    public class RerollShopTileBehavior : MonoBehaviour
    {
        static readonly int CLICK_OK_HASH   = AudioManager.BUTTON_CLICK_HASH;
        static readonly int CLICK_DENY_HASH = "BuyDeny".GetHashCode();

        [Header("UI")]
        [SerializeField] Image    iconImage;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text amountLabel;
        [SerializeField] Button   buyButton;
        [SerializeField] TMP_Text priceLabel;
        [SerializeField] Sprite   enabledBtnSprite;
        [SerializeField] Sprite   disabledBtnSprite;

        CurrencySave goldSave;

        void Start()
        {
            goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
            goldSave.onGoldAmountChanged += _ => RefreshButton();

            iconImage.sprite = GameController.CurrenciesManager.GetIcon("Reroll");
            titleLabel.text  = "REROLL";

            buyButton.onClick.AddListener(OnBuyClicked);

            RefreshAll();
        }

        void OnDestroy()
        {
            if (goldSave != null)
                goldSave.onGoldAmountChanged -= _ => RefreshButton();
            buyButton.onClick.RemoveListener(OnBuyClicked);
            RerollManager.OnStackChanged -= OnStackChanged;
        }

        /* ─── UI refresh ─── */
        void RefreshAll()
        {
            RerollManager.OnStackChanged -= OnStackChanged;
            RerollManager.OnStackChanged += OnStackChanged;
            OnStackChanged(RerollManager.Instance.Stack);
        }

        void OnStackChanged(int stack)
        {
            amountLabel.text = $"Owned {stack}x";               // ← wording update
            priceLabel.text  = RerollManager.Instance.MenuPrice.ToString();
            RefreshButton();
        }

        void RefreshButton()
        {
            bool canBuy = goldSave.CanAfford(RerollManager.Instance.MenuPrice);
            buyButton.interactable = canBuy;
            buyButton.image.sprite = canBuy ? enabledBtnSprite : disabledBtnSprite;
        }

        void OnBuyClicked()
        {
            bool ok = RerollManager.Instance.TryBuyInMenu();
            GameController.AudioManager.PlaySound(ok ? CLICK_OK_HASH : CLICK_DENY_HASH);

            EventSystem.current.SetSelectedGameObject(buyButton.gameObject);

            if (ok) OnStackChanged(RerollManager.Instance.Stack);
        }
    }
}
