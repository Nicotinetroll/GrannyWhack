using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OctoberStudio;
using OctoberStudio.Abilities;
using OctoberStudio.Save;
using OctoberStudio.Upgrades;
using OctoberStudio.UI;    // ← for CharacterXPBar

namespace OctoberStudio.UI
{
    public class SelectedCharacterItemBehavior : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] private Image    iconImage;
        [SerializeField] private TMP_Text titleLabel;

        [Header("Stats")]
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text levelLabel;

        [Header("Starting Ability")]
        [SerializeField] private GameObject abilityIconContainer;
        [SerializeField] private Image      abilityIconImage;

        [Header("XP Bar")]                            // ← now uses CharacterXPBar
        [SerializeField] private CharacterXPBar xpBar;

        [Header("Next Unlock")]
        [SerializeField] private TMP_Text nextUnlockLabel;

        [Header("Total Play Time")]
        [SerializeField] private TMP_Text playtimeText;

        [Header("Upgrades (optional)")]
        [SerializeField] private UpgradesDatabase upgradesDatabase;

        // runtime state
        private UpgradesSave      upgradesSave;
        private CharacterData     currentData;
        private AbilitiesDatabase currentDb;

        /// <summary>
        /// Call this from your lobby to wire up the display.
        /// </summary>
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            currentData = data;
            currentDb   = db;

            // icon & name
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            // listen for shop upgrades
            if (upgradesSave == null)
            {
                upgradesSave = GameController
                                  .SaveManager
                                  .GetSave<UpgradesSave>("Upgrades Save");
                upgradesSave.Init();
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            // ensure our play‑time tracker is running
            CharacterPlaytimeSystem.Init(GameController.SaveManager);

            // draw the whole card
            RedrawAll();
        }

        private void RedrawAll()
        {
            // HP
            hpText.text = currentData.BaseHP.ToString("F0");

            // Base + level bonus
            float basePlusLevel = currentData.BaseDamage
                               + CharacterLevelSystem.GetDamageBonus(currentData);

            // Shop “damage” multiplier
            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");

            float multiplier = 1f;
            var upgDef = upgradesDatabase?.GetUpgrade(UpgradeType.Damage);
            if (upgDef != null && upgDef.LevelsCount > 0)
            {
                int shopLevel = GameController.UpgradesManager
                                             .GetUpgradeLevel(UpgradeType.Damage);
                if (shopLevel > 0)
                {
                    // level 1 → index 0, level 2 → index 1, etc.
                    int idx = Mathf.Clamp(shopLevel - 1, 0, upgDef.LevelsCount - 1);
                    multiplier = upgDef.GetLevel(idx).Value;
                }
            }

            damageText.text = (basePlusLevel * multiplier)
                                 .ToString("F1");

            // Level number
            int lvl = CharacterLevelSystem.GetLevel(currentData);
            levelLabel.text = $"{lvl}";

            // Starting‑ability icon
            bool has = currentData.HasStartingAbility;
            abilityIconContainer.SetActive(has);
            if (has && currentDb != null)
            {
                var ad = currentDb.GetAbility(currentData.StartingAbility);
                abilityIconImage.sprite = ad.Icon;
            }

            // **Slider‑based XP bar**
            xpBar?.Setup(currentData);

            // Next‑unlock text
            if (nextUnlockLabel != null && currentDb != null)
            {
                var unlockLevels = Enumerable
                    .Range(0, currentDb.AbilitiesCount)
                    .Select(i => currentDb.GetAbility(i))
                    .Where(ad => ad != null
                              && ad.IsEvolution
                              && ad.IsCharacterSpecific
                              && ad.AllowedCharacterName == currentData.Name)
                    .Select(ad => ad.MinCharacterLevel)
                    .Distinct()
                    .OrderBy(x => x);

                int next = unlockLevels.FirstOrDefault(x => x > lvl);
                nextUnlockLabel.text = next > 0
                    ? $"Next unlock at level {next}."
                    : "All EVO abilities unlocked!";
            }

            // Total play time
            if (playtimeText != null)
            {
                float sec = CharacterPlaytimeSystem.GetTime(currentData);
                int mm = Mathf.FloorToInt(sec / 60f);
                int ss = Mathf.FloorToInt(sec % 60f);
                playtimeText.text = $"{mm:00}:{ss:00}";
            }
        }

        private void OnUpgradeLevelChanged(UpgradeType type, int _)
        {
            if (type == UpgradeType.Damage)
                RedrawAll();
        }

        private void OnDestroy()
        {
            if (upgradesSave != null)
                upgradesSave.onUpgradeLevelChanged -= OnUpgradeLevelChanged;
        }
    }
}
