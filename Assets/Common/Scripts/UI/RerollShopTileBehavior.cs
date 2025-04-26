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
        [SerializeField] private Image    iconImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private Button   buyButton;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Sprite   enabledBtnSprite;
        [SerializeField] private Sprite   disabledBtnSprite;

        private CurrencySave goldSave;
        private RerollManager rerollManager;

        void Start()
        {
            goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
            if (goldSave != null)
                goldSave.onGoldAmountChanged += OnGoldChanged;

            rerollManager = RerollManager.Instance;

            RerollManager.OnStackChanged += OnStackChanged; // 🔥 (STATIC event!)

            if (iconImage != null)
                iconImage.sprite = GameController.CurrenciesManager.GetIcon("Reroll");

            if (titleLabel != null)
                titleLabel.text = "REROLL";

            if (buyButton != null)
                buyButton.onClick.AddListener(OnBuyClicked);

            RefreshAll();
        }

        void OnDestroy()
        {
            if (goldSave != null)
                goldSave.onGoldAmountChanged -= OnGoldChanged;

            if (buyButton != null)
                buyButton.onClick.RemoveListener(OnBuyClicked);

            RerollManager.OnStackChanged -= OnStackChanged; // 🔥 (STATIC event!)
        }

        void RefreshAll()
        {
            if (rerollManager != null)
                OnStackChanged(rerollManager.Stack);
        }

        void OnStackChanged(int stack)
        {
            if (amountLabel != null)
                amountLabel.text = $"Owned {stack} rerolls";

            if (priceLabel != null && rerollManager != null)
                priceLabel.text = rerollManager.MenuPrice.ToString();

            RefreshButton();
        }

        void RefreshButton()
        {
            if (buyButton == null || rerollManager == null)
                return;

            bool canBuy = goldSave != null && goldSave.CanAfford(rerollManager.MenuPrice);

            buyButton.interactable = canBuy;

            if (buyButton.image != null)
                buyButton.image.sprite = canBuy ? enabledBtnSprite : disabledBtnSprite;
        }

        void OnGoldChanged(int _)
        {
            RefreshButton();
        }

        void OnBuyClicked()
        {
            if (rerollManager == null || buyButton == null)
                return;

            bool ok = rerollManager.TryBuyInMenu();
            GameController.AudioManager.PlaySound(ok ? CLICK_OK_HASH : CLICK_DENY_HASH);

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(buyButton.gameObject);

            if (ok)
                OnStackChanged(rerollManager.Stack);
        }
    }
}
