using System;
using OctoberStudio.Abilities;
using OctoberStudio.Audio;
using OctoberStudio.Save;
using OctoberStudio.Upgrades;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

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
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Sprite enabledButtonSprite;
        [SerializeField] private Sprite disabledButtonSprite;
        [SerializeField] private Sprite selectedButtonSprite;

        [Header("Stats")]
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text levelText;

        [Header("Upgrades (optional)")]
        [SerializeField] private UpgradesDatabase upgradesDatabase;

        [Space]
        [SerializeField] private ScalingLabelBehavior costLabel;
        [SerializeField] private TMP_Text buttonText;

        private CharactersSave charactersSave;
        private UpgradesSave   upgradesSave;
        public CurrencySave    GoldCurrency { get; private set; }

        public CharacterData Data { get; private set; }
        public int           CharacterId { get; private set; }

        public Selectable Selectable => upgradeButton;
        public UnityAction<CharacterItemBehavior> onNavigationSelected;

        // track nav focus
        private bool IsSelected;

        private void Start()
        {
            upgradeButton.onClick.AddListener(SelectButtonClick);
        }

        public void Init(int id, CharacterData characterData, AbilitiesDatabase database)
        {
            Data        = characterData;
            CharacterId = id;

            // subscribe to character selection changes
            if (charactersSave == null)
            {
                charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
                charactersSave.onSelectedCharacterChanged += RedrawVisuals;
            }

            // subscribe to gold changes
            if (GoldCurrency == null)
            {
                GoldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");
                GoldCurrency.onGoldAmountChanged += OnGoldAmountChanged;
            }

            // subscribe to upgrades
            if (upgradesSave == null)
            {
                upgradesSave = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades");
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            // starting ability icon
            startingAbilityObject.SetActive(characterData.HasStartingAbility);
            if (characterData.HasStartingAbility && database != null)
            {
                var ad = database.GetAbility(characterData.StartingAbility);
                startingAbilityImage.sprite = ad.Icon;
            }

            RedrawVisuals();
        }

        private void RedrawVisuals()
        {
            // Icon & Name
            titleLabel.text  = Data.Name;
            iconImage.sprite = Data.Icon;

            // Level
            if (levelText != null)
            {
                int lvl = CharacterLevelSystem.GetLevel(Data);
                levelText.text = $"Level {lvl}";
            }

            // HP
            hpText.text = Data.BaseHP.ToString("F0");

            // DAMAGE
            float basePlusLevel = Data.BaseDamage + CharacterLevelSystem.GetDamageBonus(Data);

            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");

            float multiplier = 1f;
            if (upgradesDatabase != null)
            {
                var upgDef = upgradesDatabase.GetUpgrade(UpgradeType.Damage);
                if (upgDef != null && upgDef.LevelsCount > 0)
                {
                    int shopLevel = GameController.UpgradesManager.GetUpgradeLevel(UpgradeType.Damage);
                    if (shopLevel > 0)
                    {
                        int idx = Mathf.Clamp(shopLevel - 1, 0, upgDef.LevelsCount - 1);
                        multiplier = upgDef.GetLevel(idx).Value;
                    }
                }
            }

            damageText.text = (basePlusLevel * multiplier).ToString("F1");

            RedrawButton();
        }

        private void RedrawButton()
        {
            bool owned = charactersSave.HasCharacterBeenBought(CharacterId);
            costLabel.gameObject.SetActive(!owned);
            buttonText.gameObject.SetActive(owned);

            if (owned)
            {
                if (charactersSave.SelectedCharacterId == CharacterId)
                {
                    upgradeButton.interactable = false;
                    upgradeButton.image.sprite = selectedButtonSprite;
                    buttonText.text = "SELECTED";
                }
                else
                {
                    upgradeButton.interactable = true;
                    upgradeButton.image.sprite = enabledButtonSprite;
                    buttonText.text = "SELECT";
                }
            }
            else
            {
                costLabel.SetAmount(Data.Cost);
                if (GoldCurrency.CanAfford(Data.Cost))
                {
                    upgradeButton.interactable = true;
                    upgradeButton.image.sprite = enabledButtonSprite;
                }
                else
                {
                    upgradeButton.interactable = false;
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

        private void OnGoldAmountChanged(int _)    => RedrawButton();
        private void OnUpgradeLevelChanged(UpgradeType type, int _) 
        {
            if (type == UpgradeType.Damage)
                RedrawVisuals();
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

        // ★ PUBLIC SELECT METHODS ★
        /// <summary>
        /// Explicitly select this item (for gamepad/keyboard nav).
        /// </summary>
        public void Select()
        {
            EventSystem.current.SetSelectedGameObject(upgradeButton.gameObject);
        }

        /// <summary>
        /// Clear the internal focus flag so it can re‑fire onNavigationSelected next time.
        /// </summary>
        public void Unselect()
        {
            IsSelected = false;
        }

        public void Clear()
        {
            if (GoldCurrency != null)
                GoldCurrency.onGoldAmountChanged -= OnGoldAmountChanged;
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= RedrawVisuals;
            if (upgradesSave != null)
                upgradesSave.onUpgradeLevelChanged -= OnUpgradeLevelChanged;
        }
    }
}
