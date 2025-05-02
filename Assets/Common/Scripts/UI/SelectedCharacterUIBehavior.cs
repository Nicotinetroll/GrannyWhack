// Assets/Common/Scripts/UI/SelectedCharacterUIBehavior.cs
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
        UpgradesSave      upgradesSave;
        CharactersSave    charactersSave;
        CharacterData     currentData;
        AbilitiesDatabase currentDb;

    /*────────────────── API ──────────────────*/
        /// <summary>
        /// Volaj vždy, keď sa zmení zvolená postava alebo jej štatistiky.
        /// </summary>
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            currentData = data;
            currentDb   = db;

            /* 1) Ikona + meno */
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            /* 2) hook na Upgrades */
            if (upgradesSave == null)
            {
                upgradesSave = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades Save");
                upgradesSave.Init();
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            /* 3) hook na CharactersSave → aby sa UI preplo aj po výbere novej postavy */
            if (charactersSave == null)
                charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");

            /* 4) play‑time & kills systémy */
            CharacterPlaytimeSystem.Init(GameController.SaveManager);
            CharacterKillSystem.Init(GameController.SaveManager);

            RedrawAll();
        }

    /*────────────────── core ──────────────────*/
        void RedrawAll()
        {
        /*–— 1) HP –—*/
            float baseHP = charactersSave != null && charactersSave.CharacterHealth > 0
                          ? charactersSave.CharacterHealth
                          : currentData.BaseHP;
            hpText.text = baseHP.ToString("F0");

        /*–— 2) Damage –—*/
            float rawDmg = charactersSave != null && charactersSave.CharacterDamage > 0
                         ? charactersSave.CharacterDamage
                         : currentData.BaseDamage + CharacterLevelSystem.GetDamageBonus(currentData);

            /* global upgrade multipler */
            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");

            float mult = 1f;
            var upDef  = upgradesDatabase?.GetUpgrade(UpgradeType.Damage);
            if (upDef != null && upDef.LevelsCount > 0)
            {
                int shopLvl = GameController.UpgradesManager.GetUpgradeLevel(UpgradeType.Damage);
                if (shopLvl > 0)
                {
                    int idx = Mathf.Clamp(shopLvl - 1, 0, upDef.LevelsCount - 1);
                    mult = upDef.GetLevel(idx).Value;
                }
            }
            damageText.text = (rawDmg * mult).ToString("F1");

        /*–— 3) Level –—*/
            int lvl = CharacterLevelSystem.GetLevel(currentData);
            levelLabel.text = $"{lvl}";

        /*–— 4) Starting ability –—*/
            bool hasSA = currentData.HasStartingAbility;
            abilityIconContainer.SetActive(hasSA);
            if (hasSA && currentDb != null)
            {
                var sa = currentDb.GetAbility(currentData.StartingAbility);
                abilityIconImage.sprite = sa?.Icon;
            }

        /*–— 5) XP bar –—*/
            xpBar?.Setup(currentData);

        /*–— 6) Next EVO unlock pomocou levelu –—*/
            if (nextUnlockLabel != null && currentDb != null)
            {
                var next = Enumerable.Range(0, currentDb.AbilitiesCount)
                                     .Select(i => currentDb.GetAbility(i))
                                     .Where(ad => ad.IsEvolution
                                               && ad.IsCharacterSpecific
                                               && ad.AllowedCharacterName == currentData.Name)
                                     .Select(ad => ad.MinCharacterLevel)
                                     .Distinct()
                                     .OrderBy(x => x)
                                     .FirstOrDefault(x => x > lvl);

                nextUnlockLabel.text = next > 0
                    ? $"Next unlock at level {next}."
                    : "All EVO abilities unlocked!";
            }

        /*–— 7) Kills & play‑time –—*/
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

    /*────────────────── events ──────────────────*/
        void OnUpgradeLevelChanged(UpgradeType t, int _) { if (t == UpgradeType.Damage) RedrawAll(); }

        void OnDestroy()
        {
            if (upgradesSave != null)
                upgradesSave.onUpgradeLevelChanged -= OnUpgradeLevelChanged;
        }
    }
}
