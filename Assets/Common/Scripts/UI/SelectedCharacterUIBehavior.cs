/******************************************************************************
 *  SelectedCharacterUIBehavior.cs                                            *
 *  Shows the currently–selected character in the lobby / character screen.   *
 *                                                                            *
 *  • Always displays the *actual* stats used in‑game:                        *
 *      –  Base stats (+ level bonus for damage)                              *
 *      –  Global shop upgrades (Damage ‑ multipliers, Health ‑ additive)     *
 *      –  DEV‑popup overrides, if any                                        *
 *  • Auto‑refreshes when either Damage *or* Health shop‑upgrade changes.     *
 ******************************************************************************/

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
    /*───────────── Inspector ─────────────*/
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

    /*───────────── runtime ─────────────*/
        UpgradesSave      upgradesSave;
        CharactersSave    charactersSave;
        CharacterData     currentData;
        AbilitiesDatabase currentDb;

    /*────────────────── API ──────────────────*/
        /// <summary>
        /// Call every time the selected character **or its stats** change.
        /// </summary>
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            currentData = data;
            currentDb   = db;

            /* 1) basic visuals */
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            /* 2) Upgrades‑save hook (for live refresh) */
            if (upgradesSave == null)
            {
                upgradesSave = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades Save");
                upgradesSave.Init();
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            /* 3) Characters‑save hook (DEV overrides) */
            if (charactersSave == null)
                charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");

            /* 4) Ensure auxiliary systems are initialised */
            CharacterPlaytimeSystem.Init(GameController.SaveManager);
            CharacterKillSystem.Init(GameController.SaveManager);

            RedrawAll();
        }

    /*────────────────── core ──────────────────*/
        void RedrawAll()
        {
        /* 1) HP ─────────────────────────────────────────────────────────── */
            float hpUpgradeBonus = GameController.UpgradesManager
                                              .GetUpgadeValue(UpgradeType.Health);

            float hpToShow = (charactersSave != null && charactersSave.CharacterHealth > 0f)
                           ? charactersSave.CharacterHealth
                           : currentData.BaseHP + hpUpgradeBonus;

            hpText.text = hpToShow.ToString("F0");

        /* 2) DAMAGE ─────────────────────────────────────────────────────── */
            float rawDamage = (charactersSave != null && charactersSave.CharacterDamage > 0f)
                            ? charactersSave.CharacterDamage
                            : currentData.BaseDamage
                              + CharacterLevelSystem.GetDamageBonus(currentData);

            /* global damage multiplier (shop) */
            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");

            float dmgMult = 1f;
            var dmgUpDef  = upgradesDatabase?.GetUpgrade(UpgradeType.Damage);
            if (dmgUpDef != null && dmgUpDef.LevelsCount > 0)
            {
                int shopLvl = GameController.UpgradesManager.GetUpgradeLevel(UpgradeType.Damage);
                if (shopLvl > 0)
                {
                    int idx = Mathf.Clamp(shopLvl - 1, 0, dmgUpDef.LevelsCount - 1);
                    dmgMult = dmgUpDef.GetLevel(idx).Value;
                }
            }
            damageText.text = (rawDamage * dmgMult).ToString("F1");

        /* 3) LEVEL ──────────────────────────────────────────────────────── */
            int lvl = CharacterLevelSystem.GetLevel(currentData);
            levelLabel.text = $"{lvl}";

        /* 4) STARTING ABILITY ───────────────────────────────────────────── */
            bool hasSA = currentData.HasStartingAbility;
            abilityIconContainer.SetActive(hasSA);
            if (hasSA && currentDb != null)
            {
                var ad = currentDb.GetAbility(currentData.StartingAbility);
                abilityIconImage.sprite = ad?.Icon;
            }

        /* 5) XP BAR ─────────────────────────────────────────────────────── */
            xpBar?.Setup(currentData);

        /* 6) NEXT EVO UNLOCK (by level) ─────────────────────────────────── */
            if (nextUnlockLabel != null && currentDb != null)
            {
                int upcoming = Enumerable.Range(0, currentDb.AbilitiesCount)
                                         .Select(i => currentDb.GetAbility(i))
                                         .Where(ad => ad.IsEvolution
                                                   && ad.IsCharacterSpecific
                                                   && ad.AllowedCharacterName == currentData.Name)
                                         .Select(ad => ad.MinCharacterLevel)
                                         .Distinct()
                                         .OrderBy(x => x)
                                         .FirstOrDefault(x => x > lvl);

                nextUnlockLabel.text = upcoming > 0
                    ? $"Next unlock at level {upcoming}."
                    : "All EVO abilities unlocked!";
            }

        /* 7) KILLS & PLAY‑TIME ─────────────────────────────────────────── */
            if (killsText != null)
                killsText.text = CharacterKillSystem.GetKills(currentData).ToString();

            if (playtimeText != null)
            {
                float seconds = CharacterPlaytimeSystem.GetTime(currentData);
                int mm = Mathf.FloorToInt(seconds / 60f);
                int ss = Mathf.FloorToInt(seconds % 60f);
                playtimeText.text = $"{mm:00}:{ss:00}";
            }
        }

    /*────────────────── events ──────────────────*/
        void OnUpgradeLevelChanged(UpgradeType type, int _)
        {
            if (type == UpgradeType.Damage || type == UpgradeType.Health)
                RedrawAll();
        }

        void OnDestroy()
        {
            if (upgradesSave != null)
                upgradesSave.onUpgradeLevelChanged -= OnUpgradeLevelChanged;
        }
    }
}
