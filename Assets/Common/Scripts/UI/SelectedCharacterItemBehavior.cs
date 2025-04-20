using OctoberStudio.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    /// <summary>
    /// Read‑only display of a CharacterData: icon, name, HP, damage, level, starting ability, and XP bar.
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
        [SerializeField] private CharacterExperienceUI xpBar;  // ← the XP‑bar component

        /// <summary>
        /// Populate using the CharacterData and fetch ability icon from the AbilitiesDatabase.
        /// </summary>
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            Debug.Log($"[SelectedDisplay] Setup called for {data.Name}. HasStartingAbility={data.HasStartingAbility}");

            // Icon & Name
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            // Base HP
            hpText.text = data.BaseHP.ToString("F0");

            // Damage = base + per‑level bonus
            float dmg = data.BaseDamage + CharacterLevelSystem.GetDamageBonus(data);
            damageText.text = dmg.ToString("F1");

            // Level
            int lvl = CharacterLevelSystem.GetLevel(data);
            levelLabel.text = $"{lvl}";

            // Starting Ability
            bool has = data.HasStartingAbility;
            abilityIconContainer.SetActive(has);
            if (has && db != null)
            {
                var ad = db.GetAbility(data.StartingAbility);
                Debug.Log($"[SelectedDisplay] Found starting ability icon={ad.Icon.name}");
                abilityIconImage.sprite = ad.Icon;
            }

            // Finally, drive the XP bar
            if (xpBar != null)
                xpBar.Setup(data);
        }
    }
}
