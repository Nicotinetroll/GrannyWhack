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
        public  CurrencySave   GoldCurrency { get; private set; }

        public  CharacterData  Data        { get; private set; }
        public  int            CharacterId { get; private set; }

        public  Selectable     Selectable => upgradeButton;
        public  UnityAction<CharacterItemBehavior> onNavigationSelected;

        bool IsSelected;

        private void Awake()
        {
            upgradeButton.onClick.AddListener(SelectButtonClick);
        }

        public void Init(int id, CharacterData characterData, AbilitiesDatabase database)
        {
            Data        = characterData;
            CharacterId = id;

            // Subscribe to character selection changes using RedrawVisuals (no args)
            if (charactersSave == null)
            {
                charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
                charactersSave.onSelectedCharacterChanged += RedrawVisuals;
            }

            // Subscribe to gold changes
            if (GoldCurrency == null)
            {
                GoldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");
                GoldCurrency.onGoldAmountChanged += _ => RedrawButton();
            }

            // Subscribe to upgrades changes
            if (upgradesSave == null)
            {
                upgradesSave = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades Save");
                upgradesSave.Init();
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            // Starting ability icon
            startingAbilityObject.SetActive(characterData.HasStartingAbility);
            if (characterData.HasStartingAbility && database != null)
            {
                var ad = database.GetAbility(characterData.StartingAbility);
                startingAbilityImage.sprite = ad.Icon;
            }

            RedrawVisuals();
        }

        private void RedrawVisuals(int _ = 0)
        {
            // Icon & Name
            if (titleLabel != null) titleLabel.text  = Data.Name;
            if (iconImage  != null) iconImage.sprite = Data.Icon;

            // Level
            if (levelText != null)
                levelText.text = CharacterLevelSystem.GetLevel(Data).ToString();

            // HP
            if (hpText != null)
                hpText.text = Data.BaseHP.ToString("F0");

            // DAMAGE
            float basePlusLevel = Data.BaseDamage + CharacterLevelSystem.GetDamageBonus(Data);

            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");

            float multiplier = 1f;
            var upgDef = upgradesDatabase?.GetUpgrade(UpgradeType.Damage);
            if (upgDef != null && upgDef.LevelsCount > 0)
            {
                int shopLevel = GameController.UpgradesManager.GetUpgradeLevel(UpgradeType.Damage);
                if (shopLevel > 0)
                {
                    int idx = Mathf.Clamp(shopLevel - 1, 0, upgDef.LevelsCount - 1);
                    multiplier = upgDef.GetLevel(idx).Value;
                }
            }

            if (damageText != null)
                damageText.text = (basePlusLevel * multiplier).ToString("F1");

            RedrawButton();
        }

        private void RedrawButton()
        {
            if (upgradeButton == null) return;

            bool owned = charactersSave != null && charactersSave.HasCharacterBeenBought(CharacterId);

            if (costLabel != null && costLabel.gameObject != null)
                costLabel.gameObject.SetActive(!owned);

            if (buttonText != null)
                buttonText.gameObject.SetActive(owned);

            if (owned)
            {
                bool isSel = charactersSave.SelectedCharacterId == CharacterId;
                upgradeButton.interactable = !isSel;

                if (upgradeButton.image != null)
                    upgradeButton.image.sprite = isSel
                        ? selectedButtonSprite
                        : enabledButtonSprite;

                if (buttonText != null)
                    buttonText.text = isSel ? "SELECTED" : "SELECT";
            }
            else
            {
                if (costLabel != null)
                    costLabel.SetAmount(Data.Cost);

                bool canAfford = GoldCurrency != null && GoldCurrency.CanAfford(Data.Cost);
                upgradeButton.interactable = canAfford;

                if (upgradeButton.image != null)
                    upgradeButton.image.sprite = canAfford
                        ? enabledButtonSprite
                        : disabledButtonSprite;
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

        public void Select()
        {
            EventSystem.current.SetSelectedGameObject(upgradeButton.gameObject);
        }

        public void Unselect()
        {
            IsSelected = false;
        }

        private void OnDestroy()
        {
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= RedrawVisuals;
            if (upgradesSave != null)
                upgradesSave.onUpgradeLevelChanged   -= OnUpgradeLevelChanged;
            if (GoldCurrency  != null)
                GoldCurrency.onGoldAmountChanged     -= _ => RedrawButton();
        }

        public void Clear() => OnDestroy();
    }
}
