using OctoberStudio.Abilities;
using OctoberStudio.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class CharacterItemBehavior : MonoBehaviour
    {
        [SerializeField] RectTransform rect;
        public RectTransform Rect => rect;

        [Header("Info")]
        [SerializeField] private Image   iconImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private GameObject startingAbilityObject;
        [SerializeField] private Image   startingAbilityImage;

        [Header("Button")]
        [SerializeField] private Button  upgradeButton;
        [SerializeField] private Sprite  enabledButtonSprite;
        [SerializeField] private Sprite  disabledButtonSprite;
        [SerializeField] private Sprite  selectedButtonSprite;

        [Header("Stats")]
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text levelLabel;

        [Space]
        [SerializeField] private ScalingLabelBehavior costLabel;
        [SerializeField] private TMP_Text           buttonText;

        public CurrencySave GoldCurrency { get; private set; }
        private CharactersSave charactersSave;

        public Selectable Selectable => upgradeButton;
        public CharacterData Data { get; private set; }
        public int CharacterId { get; private set; }
        public bool IsSelected { get; private set; }

        public UnityAction<CharacterItemBehavior> onNavigationSelected;

        private void Start()
        {
            upgradeButton.onClick.AddListener(SelectButtonClick);
        }

        /// <summary>
        /// Call once to initialize this item in your shop/selection UI.
        /// </summary>
        public void Init(int id, CharacterData characterData, AbilitiesDatabase database)
        {
            Data        = characterData;
            CharacterId = id;

            // subscribe to SelectedCharacterChanged
            if (charactersSave == null)
            {
                charactersSave = GameController
                                   .SaveManager
                                   .GetSave<CharactersSave>("Characters");
                charactersSave.onSelectedCharacterChanged += OnSelectedCharacterChanged;
            }

            // subscribe to gold changes
            if (GoldCurrency == null)
            {
                GoldCurrency = GameController
                                   .SaveManager
                                   .GetSave<CurrencySave>("gold");
                GoldCurrency.onGoldAmountChanged += OnGoldAmountChanged;
            }

            // setup starting ability icon
            startingAbilityObject.SetActive(characterData.HasStartingAbility);
            if (characterData.HasStartingAbility)
            {
                var abilityData = database.GetAbility(characterData.StartingAbility);
                startingAbilityImage.sprite = abilityData.Icon;
            }

            RedrawVisuals();
        }

        private void OnSelectedCharacterChanged()
        {
            RedrawVisuals();
        }

        private void OnGoldAmountChanged(int _)
        {
            RedrawVisuals();
        }

        private void RedrawVisuals()
        {
            // guard against destroyed UI
            if (titleLabel == null) return;

            titleLabel.text  = Data.Name;
            iconImage.sprite = Data.Icon;
            hpText.text      = Data.BaseHP.ToString("F0");

            float dmg        = Data.BaseDamage + CharacterLevelSystem.GetDamageBonus(Data);
            damageText.text  = dmg.ToString("F2");

            if (levelLabel != null)
                levelLabel.text = $"{CharacterLevelSystem.GetLevel(Data)}";

            RedrawButton();
        }

        private void RedrawButton()
        {
            // guard
            if (upgradeButton == null) return;

            if (charactersSave.HasCharacterBeenBought(CharacterId))
            {
                costLabel?.gameObject.SetActive(false);
                buttonText?.gameObject.SetActive(true);

                if (charactersSave.SelectedCharacterId == CharacterId)
                {
                    upgradeButton.interactable      = false;
                    upgradeButton.image.sprite     = selectedButtonSprite;
                    buttonText.text                = "SELECTED";
                }
                else
                {
                    upgradeButton.interactable      = true;
                    upgradeButton.image.sprite     = enabledButtonSprite;
                    buttonText.text                = "SELECT";
                }
            }
            else
            {
                costLabel?.gameObject.SetActive(true);
                buttonText?.gameObject.SetActive(false);

                costLabel?.SetAmount(Data.Cost);

                if (GoldCurrency.CanAfford(Data.Cost))
                {
                    upgradeButton.interactable  = true;
                    upgradeButton.image.sprite = enabledButtonSprite;
                }
                else
                {
                    upgradeButton.interactable  = false;
                    upgradeButton.image.sprite = disabledButtonSprite;
                }
            }
        }

        private void SelectButtonClick()
        {
            if (!charactersSave.HasCharacterBeenBought(CharacterId))
            {
                GoldCurrency.Withdraw(Data.Cost);
                charactersSave.AddBoughtCharacter(CharacterId);
            }

            charactersSave.SetSelectedCharacterId(CharacterId);

            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            EventSystem.current.SetSelectedGameObject(upgradeButton.gameObject);
        }

        public void Select()
        {
            EventSystem.current.SetSelectedGameObject(upgradeButton.gameObject);
        }

        public void Unselect()
        {
            IsSelected = false;
        }

        private void Update()
        {
            if (!IsSelected && EventSystem.current.currentSelectedGameObject == upgradeButton.gameObject)
            {
                IsSelected = true;
                onNavigationSelected?.Invoke(this);
            }
            else if (IsSelected && EventSystem.current.currentSelectedGameObject != upgradeButton.gameObject)
            {
                IsSelected = false;
            }
        }

        public void Clear()
        {
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= OnSelectedCharacterChanged;
            if (GoldCurrency != null)
                GoldCurrency.onGoldAmountChanged   -= OnGoldAmountChanged;
        }

        private void OnDestroy()
        {
            // ensure cleanup
            Clear();
        }
    }
}
