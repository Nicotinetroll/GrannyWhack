using System.Linq;
using OctoberStudio.Abilities;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        /// <summary>
        /// Populates the display.  Pass in the full AbilitiesDatabase
        /// so we can compute the very next EVO unlock level.
        /// </summary>
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            // Icon & Name
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            // HP & Damage
            hpText.text = data.BaseHP.ToString("F0");
            float dmg = data.BaseDamage + CharacterLevelSystem.GetDamageBonus(data);
            damageText.text = dmg.ToString("F1");

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
            if (xpBar != null)
                xpBar.Setup(data);

            // Next‑unlock text
            if (nextUnlockLabel != null && db != null)
            {
                // find all EVO abilities for exactly this character
                var nextLevels =
                    Enumerable.Range(0, db.AbilitiesCount)
                              .Select(i => db.GetAbility(i))
                              .Where(ad => ad != null
                                        && ad.IsEvolution
                                        && ad.IsCharacterSpecific
                                        && ad.AllowedCharacterName == data.Name)
                              .Select(ad => ad.MinCharacterLevel)
                              .Distinct()
                              .OrderBy(x => x);

                // pick the first one above the current level
                var upcoming = nextLevels.FirstOrDefault(x => x > lvl);
                if (upcoming > 0)
                {
                    nextUnlockLabel.text = $"Next unlock at level {upcoming}.";
                }
                else
                {
                    nextUnlockLabel.text = "All EVO abilities unlocked!";
                }
            }
        }
    }
}
