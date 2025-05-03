// Assets/Common/Scripts/UI/SelectedCharacterUIBehavior.cs
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
        /*─────────────── Inspector ───────────────*/
        [Header("Info")]
        [SerializeField] Image    iconImage;
        [SerializeField] TMP_Text titleLabel;

        [Header("Stats")]
        [SerializeField] TMP_Text hpText;
        [SerializeField] TMP_Text damageText;
        [SerializeField] TMP_Text levelLabel;

        [Header("Starting Ability")]
        [SerializeField] GameObject abilityIconContainer;
        [SerializeField] Image      abilityIconImage;

        [Header("XP Bar")]
        [SerializeField] CharacterXPBar xpBar;

        [Header("Next Unlock")]
        [SerializeField] TMP_Text nextUnlockLabel;

        [Header("Total Play Time and Kills")]
        [SerializeField] TMP_Text playtimeText;
        [SerializeField] TMP_Text killsText;

        [Header("Upgrades (optional)")]
        [SerializeField] UpgradesDatabase upgradesDatabase;

        /*─────────────── runtime ───────────────*/
        UpgradesSave   upgradesSave;
        CharactersSave charactersSave;  // ← ensure this is set
        CharacterData  currentData;
        AbilitiesDatabase currentDb;

        /*────────────────── API ──────────────────*/
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            currentData = data;
            currentDb   = db;

            // Make sure we have a CharactersSave
            if (charactersSave == null)
            {
                charactersSave = GameController.SaveManager
                                   .GetSave<CharactersSave>("Characters");
                charactersSave.Init();
                // Re-draw whenever the selected character changes
                charactersSave.onSelectedCharacterChanged += _ => RedrawAll();
            }

            // Icon & Name
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            // Hook into global upgrades
            if (upgradesSave == null)
            {
                upgradesSave = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades Save");
                upgradesSave.Init();
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            // Ensure playtime/kill trackers
            CharacterPlaytimeSystem.Init(GameController.SaveManager);
            CharacterKillSystem    .Init(GameController.SaveManager);

            RedrawAll();
        }

        /*────────────────── core ──────────────────*/
        void RedrawAll()
        {
            // 1) HP
            float hpUpgrade = GameController.UpgradesManager
                                .GetUpgadeValue(UpgradeType.Health);
            float hpToShow = (charactersSave.CharacterHealth > 0f)
                           ? charactersSave.CharacterHealth
                           : currentData.BaseHP + hpUpgrade;
            hpText.text = hpToShow.ToString("F0");

            // 2) DAMAGE
            float rawBase = (charactersSave.CharacterDamage > 0f)
                          ? charactersSave.CharacterDamage
                          : currentData.BaseDamage + CharacterLevelSystem.GetDamageBonus(currentData);
            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");
            float dmgMult = 1f;
            var dmgDef    = upgradesDatabase?.GetUpgrade(UpgradeType.Damage);
            if (dmgDef != null && dmgDef.LevelsCount > 0)
            {
                int shopLvl = GameController.UpgradesManager.GetUpgradeLevel(UpgradeType.Damage);
                if (shopLvl > 0)
                {
                    int idx = Mathf.Clamp(shopLvl - 1, 0, dmgDef.LevelsCount - 1);
                    dmgMult = dmgDef.GetLevel(idx).Value;
                }
            }
            damageText.text = (rawBase * dmgMult).ToString("F1");

            // 3) LEVEL
            int lvl = CharacterLevelSystem.GetLevel(currentData);
            levelLabel.text = $"{lvl}";

            // 4) STARTING ABILITY
            bool hasSA = currentData.HasStartingAbility;
            abilityIconContainer.SetActive(hasSA);
            if (hasSA && currentDb != null)
            {
                var sa = currentDb.GetAbility(currentData.StartingAbility);
                abilityIconImage.sprite = sa?.Icon;
            }

            // 5) XP BAR
            xpBar?.Setup(currentData);

            // 6) NEXT EVO UNLOCK
            if (nextUnlockLabel != null && currentDb != null)
            {
                int upcoming = Enumerable.Range(0, currentDb.AbilitiesCount)
                    .Select(i => currentDb.GetAbility(i))
                    .Where(ad =>
                        ad.IsEvolution &&
                        ad.IsCharacterSpecific &&
                        ad.AllowedCharacterName == currentData.Name
                    )
                    .Select(ad => ad.MinCharacterLevel)
                    .Distinct()
                    .OrderBy(x => x)
                    .FirstOrDefault(x => x > lvl);

                nextUnlockLabel.text = upcoming > 0
                    ? $"Next unlock at level {upcoming}."
                    : "All EVO abilities unlocked!";
            }

            // 7) KILLS & PLAY-TIME
            if (killsText != null)
                killsText.text = CharacterKillSystem.GetKills(currentData).ToString();

            if (playtimeText != null)
            {
                float sec = CharacterPlaytimeSystem.GetTime(currentData);
                int mm = Mathf.FloorToInt(sec / 60f);
                int ss = Mathf.FloorToInt(sec % 60f);
                playtimeText.text = $"{mm:00}:{ss:00}";
            }
        }

        /*────────────────── events ──────────────────*/
        void OnUpgradeLevelChanged(UpgradeType t, int _)
        {
            if (t == UpgradeType.Damage) RedrawAll();
        }

        void OnDestroy()
        {
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= _ => RedrawAll();
            if (upgradesSave   != null)
                upgradesSave.onUpgradeLevelChanged   -= OnUpgradeLevelChanged;
        }
    }
}
