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

        /*─────────────── runtime ───────────────*/
        private UpgradesSave   upgradesSave;
        private CharactersSave charactersSave;
        private CharacterData  currentData;
        private AbilitiesDatabase currentDb;

        /*────────────────── API ──────────────────*/
        public void Setup(CharacterData data, AbilitiesDatabase db)
        {
            currentData = data;
            currentDb   = db;

            // 1) CharactersSave – inicializuj a při změně postavy překresli
            if (charactersSave == null)
            {
                charactersSave = GameController.SaveManager
                                     .GetSave<CharactersSave>("Characters");
                charactersSave.Init();
                charactersSave.onSelectedCharacterChanged += OnCharacterChanged;
            }

            // 2) Ikona a jméno
            iconImage.sprite = data.Icon;
            titleLabel.text  = data.Name;

            // 3) UpgradesSave – inicializuj a při změně upgradu překresli
            if (upgradesSave == null)
            {
                upgradesSave = GameController.SaveManager
                                  .GetSave<UpgradesSave>("Upgrades Save");
                upgradesSave.Init();
                upgradesSave.onUpgradeLevelChanged += OnUpgradeLevelChanged;
            }

            // 4) Playtime / kills systémy
            CharacterPlaytimeSystem.Init(GameController.SaveManager);
            CharacterKillSystem   .Init(GameController.SaveManager);

            RedrawAll();
        }

        private void OnCharacterChanged(int newId)
        {
            RedrawAll();
        }

        /*────────────────── core ──────────────────*/
        private void RedrawAll()
        {
            // 1) HP
            float hpShopBonus = GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Health);
            float hpOverride  = charactersSave.CharacterHealth;
            float hpBase      = currentData.BaseHP;
            float hpToShow    = hpOverride > 0f
                              ? hpOverride
                              : hpBase + hpShopBonus;
            hpText.text = hpToShow.ToString("F0");

            // 2) DAMAGE
            float dmgOverride = charactersSave.CharacterDamage;
            float dmgBase     = currentData.BaseDamage + CharacterLevelSystem.GetDamageBonus(currentData);
            float rawDmg      = dmgOverride > 0f ? dmgOverride : dmgBase;

            if (upgradesDatabase == null)
                upgradesDatabase = Resources.Load<UpgradesDatabase>("UpgradesDatabase");

            float dmgMult = 1f;
            var def       = upgradesDatabase.GetUpgrade(UpgradeType.Damage);
            if (def != null && def.LevelsCount > 0)
            {
                int lvl = GameController.UpgradesManager.GetUpgradeLevel(UpgradeType.Damage);
                if (lvl > 0)
                {
                    int idx = Mathf.Clamp(lvl - 1, 0, def.LevelsCount - 1);
                    dmgMult = def.GetLevel(idx).Value;
                }
            }
            damageText.text = (rawDmg * dmgMult).ToString("F1");

            // 3) LEVEL
            int charLvl = CharacterLevelSystem.GetLevel(currentData);
            levelLabel.text = $"{charLvl}";

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
                        ad.AllowedCharacterName == currentData.Name)
                    .Select(ad => ad.MinCharacterLevel)
                    .Distinct()
                    .OrderBy(x => x)
                    .FirstOrDefault(x => x > charLvl);

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
        private void OnUpgradeLevelChanged(UpgradeType t, int _)
        {
            if (t == UpgradeType.Damage || t == UpgradeType.Health)
                RedrawAll();
        }

        private void OnDestroy()
        {
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= OnCharacterChanged;
            if (upgradesSave != null)
                upgradesSave.onUpgradeLevelChanged   -= OnUpgradeLevelChanged;
        }
    }
}
