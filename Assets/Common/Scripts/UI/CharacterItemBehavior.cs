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
        [SerializeField] private TMP_Text levelText;  // ★ New for level display

        [Header("Upgrades (optional)")]
        [Tooltip("Global upgrades that apply to all characters.\n" +
                 "If left blank, will load from Resources/UpgradesDatabase.")]
        [SerializeField] private UpgradesDatabase upgradesDatabase;

        [Space]
        [SerializeField] private ScalingLabelBehavior costLabel;
        [SerializeField] private TMP_Text buttonText;

        public CurrencySave   GoldCurrency { get; private set; }
        private CharactersSave charactersSave;

        public Selectable Selectable => upgradeButton;

        public CharacterData Data { get; private set; }
        public int           CharacterId { get; private set; }

        public bool IsSelected { get; private set; }

        public UnityAction<CharacterItemBehavior> onNavigationSelected;

        private void Start()
        {
            upgradeButton.onClick.AddListener(SelectButtonClick);
        }

        public void Init(int id, CharacterData characterData, AbilitiesDatabase database)
        {
            Data = characterData;
            CharacterId = id;

            // Subscribe to when selected character changes (to update visuals)
            if (charactersSave == null)
            {
                charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
                charactersSave.onSelectedCharacterChanged += RedrawVisuals;
            }

            // Subscribe to gold changes (to update buy/select button)
            if (GoldCurrency == null)
            {
                GoldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");
                GoldCurrency.onGoldAmountChanged += OnGoldAmountChanged;
            }

            // Starting ability icon
            startingAbilityObject.SetActive(characterData.HasStartingAbility);
            if (characterData.HasStartingAbility && database != null)
            {
                var ad = database.GetAbility(characterData.StartingAbility);
                startingAbilityImage.sprite = ad.Icon;
            }

            // Initial draw
            RedrawVisuals();
        }

        private void RedrawVisuals()
        {
            // Icon & Title
            titleLabel.text  = Data.Name;
            iconImage.sprite = Data.Icon;

            // Level display
            if (levelText != null)
            {
                int lvl = CharacterLevelSystem.GetLevel(Data);
                levelText.text = $"Level {lvl}";
            }

            // HP
            hpText.text = Data.BaseHP.ToString("F0");

            // —— DAMAGE CALCULATION —— //

            // 1) base + level bonus
            float basePlusLevel = Data.BaseDamage + CharacterLevelSystem.GetDamageBonus(Data);

            // 2) lazy‑load upgradesDatabase if needed
            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");

            // 3) compute multiplier (default=1, apply only if shopLevel > 0)
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

            // 4) final effective damage
            float effectiveDamage = basePlusLevel * multiplier;
            damageText.text = effectiveDamage.ToString("F1");

            // —— END DAMAGE —— //

            // Refresh buy/select button
            RedrawButton();
        }

        private void RedrawButton()
        {
            bool owned = charactersSave.HasCharacterBeenBought(CharacterId);

            costLabel.gameObject.SetActive(!owned);
            buttonText.gameObject.SetActive(owned);

            if (owned)
            {
                // already owned: show SELECT / SELECTED
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
                // not owned: show COST and buy button
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
            // Buy if needed
            if (!charactersSave.HasCharacterBeenBought(CharacterId))
            {
                GoldCurrency.Withdraw(Data.Cost);
                charactersSave.AddBoughtCharacter(CharacterId);
            }

            // Select character
            charactersSave.SetSelectedCharacterId(CharacterId);
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            // Re‑focus this button
            EventSystem.current.SetSelectedGameObject(upgradeButton.gameObject);
        }

        private void OnGoldAmountChanged(int amount)
        {
            // Gold changed → maybe enable/disable buy button
            RedrawButton();
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
            // Track navigation focus to fire onNavigationSelected once
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
            if (GoldCurrency != null)
                GoldCurrency.onGoldAmountChanged -= OnGoldAmountChanged;
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= RedrawVisuals;
        }
    }
}
