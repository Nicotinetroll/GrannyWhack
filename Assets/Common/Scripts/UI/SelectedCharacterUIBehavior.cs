using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OctoberStudio;
using OctoberStudio.Abilities;
using OctoberStudio.Save;
using OctoberStudio.Upgrades;

namespace OctoberStudio.UI
{
    public class SelectedCharacterUIBehavior : MonoBehaviour
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
        [SerializeField] private CharacterXPBar xpBar;

        [Header("Next Unlock")]
        [SerializeField] private TMP_Text nextUnlockLabel;

        [Header("Total Play Time and Kills")]
        [SerializeField] private TMP_Text playtimeText;
        [SerializeField] private TMP_Text killsText;

        [Header("Upgrades (optional)")]
        [SerializeField] private UpgradesDatabase upgradesDatabase;

        private UpgradesSave      upgradesSave;
        private CharacterData     currentData;
        private AbilitiesDatabase currentDb;
        

        /// <summary>
        /// Call once to populate; will auto‐refresh on damage‐upgrade changes.
        /// </summary>
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            currentData = data;
            currentDb   = db;

            // Icon & name
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            // Subscribe to global upgrades
            if (upgradesSave == null)
            {
                upgradesSave = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades Save");
                upgradesSave.Init();
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            // Ensure the playtime tracker is initialized
            CharacterPlaytimeSystem.Init(GameController.SaveManager);
            CharacterKillSystem.Init(GameController.SaveManager);


            RedrawAll();
        }

        private void RedrawAll()
        {
            // HP
            hpText.text = currentData.BaseHP.ToString("F0");

            // DAMAGE = base + level bonus + global multiplier
            float basePlusLevel = currentData.BaseDamage + CharacterLevelSystem.GetDamageBonus(currentData);

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
            if (killsText != null)
            {
                int kills = CharacterKillSystem.GetKills(currentData);
                killsText.text = kills.ToString();
            }


            damageText.text = (basePlusLevel * multiplier).ToString("F1");

            // LEVEL
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

            // XP BAR
            xpBar?.Setup(currentData);

            // NEXT EVO UNLOCK
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

            // TOTAL PLAYTIME
            if (playtimeText != null)
            {
                float seconds = CharacterPlaytimeSystem.GetTime(currentData);
                int mm = Mathf.FloorToInt(seconds / 60f);
                int ss = Mathf.FloorToInt(seconds % 60f);
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
