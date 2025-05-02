/************************************************************
 *  DevPopupBehavior.cs – DEV panel in Lobby   (2025‑05‑04)
 *  • Add / delete gold
 *  • Live‑tweak Level, Damage, HP (sliders)
 *  • Hard‑reset (wipes *all* progress: currency, upgrades,
 *    rerolls, play‑time, kills, characters, PlayerPrefs,
 *    persistent save‑files) and reloads the scene
 *  • Broadcasts UI refresh to SelectedCharacterUIBehavior
 *    & EvoAbilitiesDisplayBehavior after every change
 ************************************************************/

using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OctoberStudio.Audio;
using OctoberStudio.Abilities;
using OctoberStudio.Save;
using OctoberStudio.Systems;
using OctoberStudio.UI;
using OctoberStudio.Upgrades;

namespace OctoberStudio.UI
{
    public sealed class DevPopupBehavior : MonoBehaviour
    {
    /*───────────────  Inspector  ───────────────*/
        [Header("Databases")]
        [SerializeField] CharactersDatabase charactersDb;
        [SerializeField] AbilitiesDatabase  abilitiesDb;

        [Header("Currency ID (must match DB)")]
        [SerializeField] string currencyID = "gold";

        [Header("Buttons")]
        [SerializeField] Button  closeBtn;
        [SerializeField] Button  addGoldBtn;
        [SerializeField] Button  deleteBtn;
        [SerializeField] Button  resetBtn;

        [Header("Sliders")]
        [SerializeField] Slider  levelSlider;  // int  1–25
        [SerializeField] Slider  dmgSlider;    // float 1–15.0
        [SerializeField] Slider  hpSlider;     // float 20–500

        [Header("Value labels")]
        [SerializeField] TMP_Text levelValLbl;
        [SerializeField] TMP_Text dmgValLbl;
        [SerializeField] TMP_Text hpValLbl;

        [Header("Original labels")]
        [SerializeField] TMP_Text origLevelLbl;
        [SerializeField] TMP_Text origDmgLbl;
        [SerializeField] TMP_Text origHpLbl;

        [Header("Gold label")]
        [SerializeField] TMP_Text goldLbl;

    /*───────────────  runtime  ───────────────*/
        CharactersSave charsSave;
        CurrencySave   curSave;
        UpgradesSave   upgSave;

        CharacterData charData;

        int   baseLvl;
        float baseDmg;
        float baseHP;

        bool popupOpen;

        InputAction escAction;
        InputAction settingsAction;

    /*──────────────────────  Awake  ─────────────────────*/
        void Awake()
        {
            charsSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            curSave   = GameController.SaveManager.GetSave<CurrencySave>(currencyID);
            upgSave   = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades Save");

            /* UI hooks */
            addGoldBtn .onClick.AddListener(AddGold);
            deleteBtn  .onClick.AddListener(DeleteEverything);
            resetBtn   .onClick.AddListener(ResetStats);
            closeBtn   .onClick.AddListener(Close);

            levelSlider.onValueChanged.AddListener(OnLevel);
            dmgSlider  .onValueChanged.AddListener(OnDmg);
            hpSlider   .onValueChanged.AddListener(OnHp);

            curSave.onGoldAmountChanged += UpdateGoldLabel;
        }

    /*──────────────────────  Open / Close  ─────────────────────*/
        public void Open()
        {
            if (popupOpen) return;
            popupOpen = true;
            gameObject.SetActive(true);

            /* Current character & true base stats */
            charData  = charactersDb.GetCharacterData(charsSave.SelectedCharacterId);
            baseLvl   = 1;
            baseDmg   = charData.BaseDamage;
            baseHP    = charData.BaseHP;

            /* Saved overrides (use only if within legal slider range) */
            int   savedLvl = CharacterLevelSystem.GetLevel(charData);

            float savedDmg = (charsSave.CharacterDamage >= 1f)
                            ? charsSave.CharacterDamage
                            : baseDmg + CharacterLevelSystem.GetDamageBonus(charData);

            float savedHP  = (charsSave.CharacterHealth >= 20f)
                            ? charsSave.CharacterHealth
                            : baseHP;

            /* Configure sliders */
            levelSlider.Configure( 1, 25,   savedLvl, true );
            dmgSlider  .Configure( 1f, 15f, savedDmg, false);
            hpSlider   .Configure(20f, 500f,savedHP , false);

            /* Labels */
            RefreshValLabels();
            origLevelLbl.text = $"Original Character Level:   {baseLvl}";
            origDmgLbl  .text = $"Original Character Damage: {baseDmg:0.0}";
            origHpLbl   .text = $"Original Character Health: {baseHP:0}";
            UpdateGoldLabel(curSave.Amount);

            /* ESC / Settings hooks */
            var asset = GameController.InputManager.InputAsset.asset;
            escAction      = asset.FindAction("Back");
            settingsAction = asset.FindAction("Settings");

            if (escAction      != null) escAction     .performed += Esc;
            if (settingsAction != null) settingsAction.performed += Esc;

            EventSystem.current.SetSelectedGameObject(closeBtn.gameObject);
        }

        void Close()
        {
            if (!popupOpen) return;
            popupOpen = false;

            if (escAction      != null) escAction     .performed -= Esc;
            if (settingsAction != null) settingsAction.performed -= Esc;

            /* Flush all modified saves */
            GameController.SaveManager.Save(true);

            gameObject.SetActive(false);
        }
        void Esc(InputAction.CallbackContext _) => Close();

    /*──────────────────────  Slider callbacks  ─────────────────────*/
        void OnLevel(float v)
        {
            int lvl = Mathf.Clamp(Mathf.RoundToInt(v), 1, 25);
            levelSlider.SetValueWithoutNotify(lvl);
            CharacterLevelSystem.SetLevel(charData, lvl);

            BroadcastUI();
            RefreshValLabels();
        }

        void OnDmg(float v)
        {
            float dmg = Mathf.Clamp(Mathf.Round(v * 10f) * 0.1f, 1f, 15f);
            dmgSlider.SetValueWithoutNotify(dmg);
            charsSave.CharacterDamage = dmg;

            BroadcastUI();
            RefreshValLabels();
        }

        void OnHp(float v)
        {
            float hp = Mathf.Clamp(Mathf.Round(v / 5f) * 5f, 20f, 500f);
            hpSlider.SetValueWithoutNotify(hp);
            charsSave.CharacterHealth = hp;

            BroadcastUI();
            RefreshValLabels();
        }

        void RefreshValLabels()
        {
            levelValLbl.text = $"Character Level:   {levelSlider.value:0}";
            dmgValLbl  .text = $"Character Damage: {dmgSlider.value:0.0}";
            hpValLbl   .text = $"Character Health: {hpSlider.value:0}";
        }

    /*──────────────────────  Buttons  ─────────────────────*/
        void AddGold()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            curSave.Deposit(1_000);                       // event updates label
        }

        /// <summary>Hard‑reset: wipes *everything* and reloads scene.</summary>
        void DeleteEverything()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            Debug.LogWarning("[DevPopup] FULL RESET initiated…");

            /* 1 ) reset each save object */
            charsSave.ResetAll();
            curSave  .ResetAll();
            upgSave ?.ResetAll();

            GameController.SaveManager.GetSave<CharacterLevelSave>("CharacterLevels")?.ResetAll();
            GameController.SaveManager.GetSave<StageSave>          ("Stage")         ?.ResetAll();
            GameController.SaveManager.GetSave<CurrencySave>       ("Reroll")        ?.ResetAll();

            /* 2 ) force‑save EMPTY data – closes file handle */
            GameController.SaveManager.Save(true);

            /* 3 ) nuke persistent data */
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            string dir = Application.persistentDataPath;
            if (Directory.Exists(dir))
            {
                foreach (string f in Directory.GetFiles(dir))
                {
                    try { File.Delete(f); }
                    catch (Exception) { /* file could be locked – ignore */ }
                }
            }

            /* 4 ) reload scene */
            Debug.LogWarning("[DevPopup] All progress wiped. Reloading…");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void ResetStats()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            CharacterLevelSystem.SetLevel(charData, baseLvl);
            charsSave.CharacterDamage = baseDmg;
            charsSave.CharacterHealth = baseHP;

            levelSlider.SetValueWithoutNotify(baseLvl);
            dmgSlider  .SetValueWithoutNotify(baseDmg);
            hpSlider   .SetValueWithoutNotify(baseHP);

            BroadcastUI();
            RefreshValLabels();
        }

    /*──────────────────────  Helpers  ─────────────────────*/
        void UpdateGoldLabel(int amt) => goldLbl.text = $"Current Golds: {amt}";

        /// <summary>Notify all onscreen UI panels to redraw.</summary>
        void BroadcastUI()
        {
            foreach (var ui in FindObjectsOfType<SelectedCharacterUIBehavior>())
                ui.Setup(charData, abilitiesDb);

            foreach (var evo in FindObjectsOfType<EvoAbilitiesDisplayBehavior>())
                evo.RefreshDisplay();
        }
    }

    /*────────── Slider extension ──────────*/
    static class SliderExt
    {
        public static void Configure(this Slider s, float min, float max, float val, bool whole)
        {
            s.wholeNumbers = whole;
            s.minValue     = min;
            s.maxValue     = max;
            s.SetValueWithoutNotify(val);
        }
    }
}
