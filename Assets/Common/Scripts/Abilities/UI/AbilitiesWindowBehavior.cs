// Assets/Common/Scripts/UI/AbilitiesWindowBehavior.cs
using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Input;
using OctoberStudio.Pool;
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
        /* ───────────────────────── runtime refs ─────────────────────────── */
        private PlayerBehavior player;
        private StageSave      stageSave;
        private AbilitiesSave  abilitiesSave;

        /* ───────────────────────‑‑ inspector refs ───────────────────────── */
        [Header("UI Elements")]
        [SerializeField] private GameObject rerollButtonPrefab;   // prefab with Button + Text
        [SerializeField] private GameObject levelUpTextObject;
        [SerializeField] private GameObject weaponSelectTextObject;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRect;
        private Vector2 panelPosition;
        private Vector2 panelHiddenPosition = Vector2.up * 2000;
        private IEasingCoroutine panelCoroutine;

        [Header("Cards")]
        [SerializeField] private GameObject    abilityCardPrefab;
        [SerializeField] private RectTransform abilitiesHolder;

        /* ────────────────────── pooled + dynamic UI ────────────────────── */
        private PoolComponent<AbilityCardBehavior> cardsPool;
        private readonly List<AbilityCardBehavior> cards = new();

        private GameObject        rerollButtonInstance;
        private Button            rerollButton;
        private TextMeshProUGUI   rerollButtonText;
        private int               rerollCharges;

        /* ───────────────────────── events out ───────────────────────────── */
        public UnityAction onPanelClosed;
        public UnityAction onPanelStartedClosing;

        /* ───────────────────── initialisation / teardown ───────────────── */
        public void Init()
        {
            cardsPool    = new PoolComponent<AbilityCardBehavior>(abilityCardPrefab, 3);
            abilitiesSave = GameController.SaveManager.GetSave<AbilitiesSave>("Abilities Save");
            stageSave     = GameController.SaveManager.GetSave<StageSave>("Stage Save");

            player        = FindFirstObjectByType<PlayerBehavior>();

            panelPosition        = panelRect.anchoredPosition;
            panelRect.anchoredPosition = panelHiddenPosition;

            // validate / clamp reroll charges
            if (stageSave.RerollCharges <= 0 || stageSave.RerollCharges > player.MaxRerollCharges)
            {
                stageSave.RerollCharges = player.MaxRerollCharges;
                stageSave.Flush();
            }
            rerollCharges = stageSave.RerollCharges;
        }

        private void OnDestroy()
        {
            cardsPool.Destroy();

            if (rerollButtonInstance != null)
                Destroy(rerollButtonInstance);
        }

        /* ─────────────────────── reroll logic ──────────────────────────── */
        private void OnRerollClicked()
        {
            if (rerollCharges > 0)
            {
                rerollCharges--;
                stageSave.RerollCharges = rerollCharges;
                stageSave.Flush();

                var newAbilities = StageController.AbilityManager.GetRandomAbilities(3);
                SetData(newAbilities);
            }
            else
            {
                Debug.Log("Out of rerolls. Show ad maybe?");
            }

            UpdateRerollButtonUI();
        }

        private void UpdateRerollButtonUI()
        {
            if (!rerollButtonText || !rerollButton) return;

            if (rerollCharges > 0)
            {
                rerollButtonText.text  = $"Reroll - {rerollCharges}";
                rerollButton.interactable = true;
            }
            else
            {
                rerollButtonText.text  = "Watch Ad to get reroll";
                rerollButton.interactable = false;
            }
        }

        public void ResetRerollCharges()
        {
            rerollCharges            = player.MaxRerollCharges;
            stageSave.RerollCharges  = rerollCharges;
            stageSave.Flush();
            UpdateRerollButtonUI();
        }

        /* ───────────────────── card / button building ──────────────────── */
        public void SetData(List<AbilityData> abilities)
        {
            // recycle old cards
            foreach (var card in cards)
            {
                card.transform.SetParent(null);
                card.gameObject.SetActive(false);
            }
            cards.Clear();

            // destroy old reroll button
            if (rerollButtonInstance != null)
            {
                Destroy(rerollButtonInstance);
                rerollButtonInstance = null;
            }

            // create reroll (if unlocked for this character level)
            if (rerollButtonPrefab != null &&
                player != null &&
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

            // ability cards
            foreach (var ability in abilities)
            {
                var card = cardsPool.GetEntity();

                card.transform.SetParent(abilitiesHolder);
                card.transform.ResetLocal();
                card.transform.SetAsLastSibling();

                card.Init(OnAbilitySelected);

                var level = abilitiesSave.GetAbilityLevel(ability.AbilityType);
                card.SetData(ability, level);

                cards.Add(card);
                
                EasingManager.DoNextFrame(() =>
                {
                    ConfigureNavigation();
                    if (cards.Count > 0)
                        EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
                });
                EasingManager.DoNextFrame(() =>
                {
                    ConfigureNavigation();

                    // focus REROLL first (if it’s on‑screen), otherwise the top card
                    GameObject firstFocus = rerollButton ? rerollButton.gameObject
                        : cards[0].gameObject;
                    EventSystem.current.SetSelectedGameObject(firstFocus);
                });
            }
        }

        /* ───────────────────────── panel show / hide ───────────────────── */
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

            /* build navigation on next frame (ensures Selectables exist) */
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

                                         foreach (var card in cards)
                                         {
                                             card.transform.SetParent(null);
                                             card.gameObject.SetActive(false);
                                         }
                                         cards.Clear();

                                         if (rerollButtonInstance != null)
                                         {
                                             Destroy(rerollButtonInstance);
                                             rerollButtonInstance = null;
                                         }

                                         gameObject.SetActive(false);
                                         onPanelClosed?.Invoke();
                                     });

            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        /* ─────────────────────── navigation wiring ─────────────────────── */
        private void ConfigureNavigation()
        {
            if (cards.Count == 0) return;

            // build basic vertical nav among cards
            for (int i = 0; i < cards.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0)                nav.selectOnUp   = cards[i - 1].Selectable;
                if (i < cards.Count - 1)  nav.selectOnDown = cards[i + 1].Selectable;
                cards[i].Selectable.navigation = nav;
            }

            // patch the reroll button into the graph
            if (rerollButton != null)
            {
                // top card → up to reroll
                var topNav = cards[0].Selectable.navigation;
                topNav.selectOnUp = rerollButton;
                cards[0].Selectable.navigation = topNav;

                // reroll → down to top card
                var rerollNav = new Navigation
                {
                    mode         = Navigation.Mode.Explicit,
                    selectOnDown = cards[0].Selectable
                };
                rerollButton.navigation = rerollNav;
            }

            // initial focus
            EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        }

        /* ─────────────────────── input callbacks ───────────────────────── */
        private void OnInputChanged(InputType prev, InputType type)
        {
            // when returning from mouse to gamepad, focus first card again
            if (prev == InputType.UIJoystick)
                EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        }

        /* ───────────────────── ability selection ───────────────────────── */
        private void OnAbilitySelected(AbilityData ability)
        {
            if (StageController.AbilityManager.IsAbilityAquired(ability.AbilityType))
            {
                var level = abilitiesSave.GetAbilityLevel(ability.AbilityType);
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
