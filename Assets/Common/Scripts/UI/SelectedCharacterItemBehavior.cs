using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OctoberStudio.Abilities;
using OctoberStudio.Save;
using OctoberStudio.Upgrades;

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

        [Header("XP Bar")]
        [SerializeField] private CharacterExperienceUI xpBar;

        [Header("Next Unlock")]
        [SerializeField] private TMP_Text nextUnlockLabel;

        [Header("Upgrades (optional)")]
        [Tooltip("Global upgrades that apply to all characters.\n" +
                 "If blank, will load from Resources/UpgradesDatabase")]
        [SerializeField] private UpgradesDatabase upgradesDatabase;

        private UpgradesSave       upgradesSave;
        private CharacterData      currentData;
        private AbilitiesDatabase  currentDb;

        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            currentData = data;
            currentDb   = db;

            // Icon & Name
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            // Subscribe to upgrades
            if (upgradesSave == null)
            {
                upgradesSave = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades");
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            RedrawAll();
        }

        private void RedrawAll()
        {
            // HP
            hpText.text = currentData.BaseHP.ToString("F0");

            // DAMAGE same logic as CharacterItemBehavior
            float basePlusLevel = currentData.BaseDamage + CharacterLevelSystem.GetDamageBonus(currentData);
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

            // Level
            int lvl = CharacterLevelSystem.GetLevel(currentData);
            levelLabel.text = $"{lvl}";

            // Starting ability
            bool has = currentData.HasStartingAbility;
            abilityIconContainer.SetActive(has);
            if (has && currentDb != null)
            {
                var ad = currentDb.GetAbility(currentData.StartingAbility);
                abilityIconImage.sprite = ad.Icon;
            }

            // XP Bar
            xpBar?.Setup(currentData);

            // Next unlock
            if (nextUnlockLabel != null && currentDb != null)
            {
                var nextLevels = Enumerable.Range(0, currentDb.AbilitiesCount)
                                           .Select(i => currentDb.GetAbility(i))
                                           .Where(ad => ad.IsEvolution
                                                     && ad.IsCharacterSpecific
                                                     && ad.AllowedCharacterName == currentData.Name)
                                           .Select(ad => ad.MinCharacterLevel)
                                           .Distinct()
                                           .OrderBy(x => x);

                int upcoming = nextLevels.FirstOrDefault(x => x > lvl);
                nextUnlockLabel.text = upcoming > 0
                    ? $"Next unlock at level {upcoming}."
                    : "All EVO abilities unlocked!";
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
