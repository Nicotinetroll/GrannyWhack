using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OctoberStudio.Abilities;
using OctoberStudio.Save;
using OctoberStudio.Upgrades;

namespace OctoberStudio.UI
{
    /// <summary>
    /// Read‑only display of a CharacterData: icon, name, HP, damage, level,
    /// starting ability, XP bar, and next‑unlock text.
    /// </summary>
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
        [Tooltip("Global upgrades that apply to all characters. " +
                 "If left blank, will load from Resources/UpgradesDatabase.")]
        [SerializeField] private UpgradesDatabase upgradesDatabase;

        /// <summary>
        /// Populates the display.
        /// </summary>
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            // Icon & Name
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            // HP
            hpText.text = data.BaseHP.ToString("F0");

            // —— DAMAGE CALCULATION —— //

            // 1) base + level bonus
            float basePlusLevel = data.BaseDamage + CharacterLevelSystem.GetDamageBonus(data);

            // 2) ensure we have an upgradesDatabase
            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");

            // 3) compute multiplier (default = 1; only apply if shopLevel > 0)
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

            // Level
            int lvl = CharacterLevelSystem.GetLevel(data);
            levelLabel.text = $"{lvl}";

            // Starting‑ability icon
            bool has = data.HasStartingAbility;
            abilityIconContainer.SetActive(has);
            if (has && db != null)
            {
                var ad = db.GetAbility(data.StartingAbility);
                abilityIconImage.sprite = ad.Icon;
            }

            // XP bar
            xpBar?.Setup(data);

            // Next‑unlock text
            if (nextUnlockLabel != null && db != null)
            {
                var nextLevels = Enumerable.Range(0, db.AbilitiesCount)
                                           .Select(i => db.GetAbility(i))
                                           .Where(ad => ad != null
                                                     && ad.IsEvolution
                                                     && ad.IsCharacterSpecific
                                                     && ad.AllowedCharacterName == data.Name)
                                           .Select(ad => ad.MinCharacterLevel)
                                           .Distinct()
                                           .OrderBy(x => x);

                int upcoming = nextLevels.FirstOrDefault(x => x > lvl);
                nextUnlockLabel.text = upcoming > 0
                    ? $"Next unlock at level {upcoming}."
                    : "All EVO abilities unlocked!";
            }
        }
    }
}
