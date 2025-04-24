using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Input;
using OctoberStudio.Pool;
using OctoberStudio.Systems;          // RerollManager
using OctoberStudio.Currency;         // CurrencySave
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace OctoberStudio.Abilities.UI
{
    public class AbilitiesWindowBehavior : MonoBehaviour
    {
        /* ────────────────── runtime refs ────────────────── */
        PlayerBehavior player;
        AbilitiesSave  abilitiesSave;
        CurrencySave   goldSave;                 // NEW

        /* ────────────────── inspector refs ──────────────── */
        [Header("UI Elements")]
        [SerializeField] GameObject rerollButtonPrefab;
        [SerializeField] Sprite     rerollEnabledSprite;     // green
        [SerializeField] Sprite     rerollDisabledSprite;    // gray
        [SerializeField] GameObject levelUpTextObject;
        [SerializeField] GameObject weaponSelectTextObject;

        [Header("Panel")]
        [SerializeField] RectTransform panelRect;
        Vector2 panelPosition;
        readonly Vector2 panelHiddenPosition = Vector2.up * 2000;
        IEasingCoroutine panelCoroutine;

        [Header("Cards")]
        [SerializeField] GameObject    abilityCardPrefab;
        [SerializeField] RectTransform abilitiesHolder;

        /* ───────── pooled + dynamic UI ───────── */
        PoolComponent<AbilityCardBehavior> cardsPool;
        readonly List<AbilityCardBehavior> cards = new();

        GameObject      rerollButtonInstance;
        Button          rerollButton;
        TextMeshProUGUI rerollButtonText;

        /* ───────────── events out ─────────────── */
        public UnityAction onPanelClosed;
        public UnityAction onPanelStartedClosing;

        /* ───────────────── init / teardown ───────────────── */
        public void Init()
        {
            cardsPool     = new PoolComponent<AbilityCardBehavior>(abilityCardPrefab, 3);
            abilitiesSave = GameController.SaveManager.GetSave<AbilitiesSave>("Abilities Save");
            player        = FindFirstObjectByType<PlayerBehavior>();

            panelPosition           = panelRect.anchoredPosition;
            panelRect.anchoredPosition = panelHiddenPosition;

            /* gold balance listener */
            goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
            goldSave.onGoldAmountChanged += _ => UpdateRerollButtonUI();

            RerollManager.OnStackChanged += _ => UpdateRerollButtonUI();
        }

        void OnDestroy()
        {
            cardsPool.Destroy();
            if (rerollButtonInstance) Destroy(rerollButtonInstance);

            goldSave.onGoldAmountChanged   -= _ => UpdateRerollButtonUI();
            RerollManager.OnStackChanged   -= _ => UpdateRerollButtonUI();
        }

        /* ────────────────── reroll logic ────────────────── */
        void OnRerollClicked()
        {
            var rm = RerollManager.Instance;
            if (rm == null) return;

            /* consume, or buy-and-consume once if empty */
            if (!rm.TryConsume())
            {
                if (!rm.TryBuyInRun()) return;   // not enough gold
                rm.TryConsume();
            }

            var newAbilities = StageController.AbilityManager.GetRandomAbilities(3);
            SetData(newAbilities);
        }

        void UpdateRerollButtonUI()
        {
            if (!rerollButtonText || !rerollButton) return;

            var rm      = RerollManager.Instance;
            int stack   = rm?.Stack      ?? 0;
            int inPrice = rm?.InRunPrice ?? 0;
            bool canPay = goldSave.CanAfford(inPrice);

            if (stack > 0)
            {
                rerollButtonText.text   = $"Rerolls left {stack}x";
                rerollButton.interactable = true;
                rerollButton.image.sprite = rerollEnabledSprite;
            }
            else
            {
                if (canPay)
                {
                    rerollButtonText.text   = $"Buy Reroll for {inPrice} golds";
                    rerollButton.interactable = true;
                    rerollButton.image.sprite = rerollEnabledSprite;
                }
                else
                {
                    rerollButtonText.text   = "No gold to buy Reroll";
                    rerollButton.interactable = false;
                    rerollButton.image.sprite = rerollDisabledSprite;
                }
            }
        }

        /* ─────────── card / button building ─────────── */
        public void SetData(List<AbilityData> abilities)
        {
            foreach (var card in cards)
            {
                card.transform.SetParent(null);
                card.gameObject.SetActive(false);
            }
            cards.Clear();

            if (rerollButtonInstance) { Destroy(rerollButtonInstance); rerollButtonInstance = null; }

            /* build reroll button if unlocked */
            if (rerollButtonPrefab && player &&
                StageController.ExperienceManager.Level >= player.RerollUnlockLevel)
            {
                rerollButtonInstance = Instantiate(rerollButtonPrefab, abilitiesHolder);
                rerollButtonInstance.transform.ResetLocal();
                rerollButtonInstance.transform.SetAsFirstSibling();

                rerollButton     = rerollButtonInstance.GetComponent<Button>();
                rerollButtonText = rerollButtonInstance.GetComponentInChildren<TextMeshProUGUI>();

                if (rerollButton)
                {
                    rerollButton.onClick.AddListener(OnRerollClicked);
                    UpdateRerollButtonUI();
                }
            }

            /* ability cards */
            foreach (var ability in abilities)
            {
                var card = cardsPool.GetEntity();
                card.transform.SetParent(abilitiesHolder);
                card.transform.ResetLocal();
                card.transform.SetAsLastSibling();

                card.Init(OnAbilitySelected);
                int level = abilitiesSave.GetAbilityLevel(ability.AbilityType);
                card.SetData(ability, level);
                cards.Add(card);

                EasingManager.DoNextFrame(() =>
                {
                    ConfigureNavigation();
                    GameObject first = rerollButton ? rerollButton.gameObject : card.gameObject;
                    EventSystem.current.SetSelectedGameObject(first);
                });
            }
        }

        /* ─────────── panel show / hide ─────────── */
        public void Show(bool isLevelUp)
        {
            Time.timeScale = 0f;
            gameObject.SetActive(true);

            levelUpTextObject.SetActive(isLevelUp);
            weaponSelectTextObject.SetActive(!isLevelUp);

            panelCoroutine.StopIfExists();
            panelCoroutine = panelRect.DoAnchorPosition(panelPosition, 0.3f)
                                     .SetEasing(EasingType.SineOut)
                                     .SetUnscaledTime(true);

            for (int i = 0; i < cards.Count; i++)
                cards[i].Show(i * 0.1f + 0.15f);

            EasingManager.DoNextFrame(ConfigureNavigation);
            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        public void Hide()
        {
            onPanelStartedClosing?.Invoke();

            panelCoroutine.StopIfExists();
            panelCoroutine = panelRect.DoAnchorPosition(panelHiddenPosition, 0.3f)
                                     .SetEasing(EasingType.SineIn)
                                     .SetUnscaledTime(true)
                                     .SetOnFinish(() =>
                                     {
                                         Time.timeScale = 1;
                                         foreach (var card in cards) { card.transform.SetParent(null); card.gameObject.SetActive(false); }
                                         cards.Clear();
                                         if (rerollButtonInstance) Destroy(rerollButtonInstance);
                                         gameObject.SetActive(false);
                                         onPanelClosed?.Invoke();
                                     });

            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        /* ─────────── navigation wiring ─────────── */
        void ConfigureNavigation()
        {
            if (cards.Count == 0) return;

            for (int i = 0; i < cards.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0)               nav.selectOnUp   = cards[i - 1].Selectable;
                if (i < cards.Count - 1) nav.selectOnDown = cards[i + 1].Selectable;
                cards[i].Selectable.navigation = nav;
            }

            if (rerollButton)
            {
                var topNav = cards[0].Selectable.navigation;
                topNav.selectOnUp = rerollButton;
                cards[0].Selectable.navigation = topNav;

                var btnNav = new Navigation
                {
                    mode         = Navigation.Mode.Explicit,
                    selectOnDown = cards[0].Selectable
                };
                rerollButton.navigation = btnNav;
            }

            EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        }

        /* ─────────── input callbacks ─────────── */
        void OnInputChanged(InputType prev, InputType type)
        {
            if (prev == InputType.UIJoystick)
                EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        }

        /* ─────────── ability selection ─────────── */
        void OnAbilitySelected(AbilityData ability)
        {
            if (StageController.AbilityManager.IsAbilityAquired(ability.AbilityType))
            {
                int level = abilitiesSave.GetAbilityLevel(ability.AbilityType);
                if (!ability.IsEndgameAbility) level++;
                if (level < 0) level = 0;

                abilitiesSave.SetAbilityLevel(ability.AbilityType, level);
                ability.Upgrade(level);
            }
            else
            {
                StageController.AbilityManager.AddAbility(ability);
            }
            Hide();
        }
    }
}
