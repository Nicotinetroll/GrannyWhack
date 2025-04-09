using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Input;
using OctoberStudio.Pool;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // ✅ Required for TextMeshPro

namespace OctoberStudio.Abilities.UI
{
    public class AbilitiesWindowBehavior : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject rerollButtonPrefab;
        private GameObject rerollButtonInstance;
        private Button rerollButton;
        private TextMeshProUGUI rerollButtonText; // ✅ TMP version

        [SerializeField] private GameObject levelUpTextObject;
        [SerializeField] private GameObject weaponSelectTextObject;

        [Space]
        [SerializeField] private RectTransform panelRect;
        private Vector2 panelPosition;
        private Vector2 panelHiddenPosition = Vector2.up * 2000;
        private IEasingCoroutine panelCoroutine;

        [Header("Cards")]
        [SerializeField] private GameObject abilityCardPrefab;
        [SerializeField] private RectTransform abilitiesHolder;

        private PoolComponent<AbilityCardBehavior> cardsPool;
        private List<AbilityCardBehavior> cards = new List<AbilityCardBehavior>();

        private AbilitiesSave abilitiesSave;

        public UnityAction onPanelClosed;
        public UnityAction onPanelStartedClosing;

        // 🔁 Reroll system
        private int rerollCharges;
        private const int maxRerollCharges = 3;
        private PlayerBehavior player;


        public void Init()
        {
            cardsPool = new PoolComponent<AbilityCardBehavior>(abilityCardPrefab, 3);
            abilitiesSave = GameController.SaveManager.GetSave<AbilitiesSave>("Abilities Save");

            panelPosition = panelRect.anchoredPosition;
            panelRect.anchoredPosition = panelHiddenPosition;

            rerollCharges = maxRerollCharges;

            // 🔍 Locate the player in scene
            player = FindObjectOfType<PlayerBehavior>();
        }


        public void ResetRerollCharges()
        {
            rerollCharges = maxRerollCharges;
            UpdateRerollButtonUI();
        }

        private void OnRerollClicked()
        {
            if (rerollCharges > 0)
            {
                rerollCharges--;
                var newAbilities = StageController.AbilityManager.GetRandomAbilities(3);
                SetData(newAbilities);
            }
            else
            {
                Debug.Log("Out of rerolls. Show ad or deny.");
            }

            UpdateRerollButtonUI();
        }

        private void UpdateRerollButtonUI()
        {
            if (rerollButtonText == null || rerollButton == null) return;

            if (rerollCharges > 0)
            {
                rerollButtonText.text = $"Reroll - {rerollCharges}";
                rerollButton.interactable = true;
            }
            else
            {
                rerollButtonText.text = "Watch Ad to get reroll";
                rerollButton.interactable = false;
            }
        }

        public void SetData(List<AbilityData> abilities)
        {
            // Clear old cards
            foreach (var card in cards)
            {
                card.transform.SetParent(null);
                card.gameObject.SetActive(false);
            }
            cards.Clear();

            // Destroy old reroll button
            if (rerollButtonInstance != null)
            {
                Destroy(rerollButtonInstance);
                rerollButtonInstance = null;
            }

            // 🧠 Only show reroll if player is level 2+
            if (rerollButtonPrefab != null && this.player != null && StageController.ExperienceManager.Level >= this.player.RerollUnlockLevel)




            {
                rerollButtonInstance = Instantiate(rerollButtonPrefab, abilitiesHolder);
                rerollButtonInstance.transform.ResetLocal();
                rerollButtonInstance.transform.SetAsFirstSibling();

                rerollButton = rerollButtonInstance.GetComponent<Button>();
                rerollButtonText = rerollButtonInstance.GetComponentInChildren<TextMeshProUGUI>();

                if (rerollButton != null)
                {
                    rerollButton.onClick.AddListener(OnRerollClicked);
                    UpdateRerollButtonUI();
                }
            }


            // Spawn ability cards
            foreach (var ability in abilities)
            {
                var card = cardsPool.GetEntity();

                card.transform.SetParent(abilitiesHolder);
                card.transform.ResetLocal();
                card.transform.SetAsLastSibling();

                card.Init(OnAbilitySelected);

                var abilityLevel = abilitiesSave.GetAbilityLevel(ability.AbilityType);
                card.SetData(ability, abilityLevel);

                cards.Add(card);
            }
        }


        public void Show(bool isLevelUp)
        {
            Time.timeScale = 0;

            gameObject.SetActive(true);

            levelUpTextObject.SetActive(isLevelUp);
            weaponSelectTextObject.SetActive(!isLevelUp);

            panelCoroutine.StopIfExists();
            panelCoroutine = panelRect.DoAnchorPosition(panelPosition, 0.3f)
                .SetEasing(EasingType.SineOut)
                .SetUnscaledTime(true);

            for (int i = 0; i < cards.Count; i++)
                cards[i].Show(i * 0.1f + 0.15f);

            EasingManager.DoNextFrame(() =>
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    var navigation = new Navigation { mode = Navigation.Mode.Explicit };

                    if (i != 0) navigation.selectOnUp = cards[i - 1].Selectable;
                    if (i != cards.Count - 1) navigation.selectOnDown = cards[i + 1].Selectable;

                    cards[i].Selectable.navigation = navigation;
                }

                EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
            });

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

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput == InputType.UIJoystick)
                EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        }

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

        private void OnDestroy()
        {
            cardsPool.Destroy();

            if (rerollButtonInstance != null)
            {
                Destroy(rerollButtonInstance);
                rerollButtonInstance = null;
            }
        }
    }
}
