/************************************************************
 *  DevPopupBehavior.cs – DEV panel in Lobby   (2025‑05‑05)
 *  ▸ fixes opening‑value & level‑change bugs
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
using OctoberStudio.Upgrades;

namespace OctoberStudio.UI
{
    public sealed class DevPopupBehavior : MonoBehaviour
    {
    /*───────── Inspector ─────────*/
        [Header("Databases")]
        [SerializeField] CharactersDatabase charactersDb;
        [SerializeField] AbilitiesDatabase  abilitiesDb;
        [SerializeField] UpgradesDatabase   upgradesDb;

        [Header("Currency key")]
        [SerializeField] string currencyID = "gold";

        [Header("Buttons")]
        [SerializeField] Button closeBtn, addGoldBtn, deleteBtn, resetBtn;

        [Header("Sliders")]
        [SerializeField] Slider levelSlider, dmgSlider, hpSlider;

        [Header("Value labels")]
        [SerializeField] TMP_Text levelValLbl, dmgValLbl, hpValLbl;

        [Header("Original labels")]
        [SerializeField] TMP_Text origLevelLbl, origDmgLbl, origHpLbl;

        [Header("Gold label")]
        [SerializeField] TMP_Text goldLbl;

    /*───────── runtime ─────────*/
        CharactersSave chars;
        CurrencySave   gold;
        UpgradesSave   upgSave;
        CharacterData  cData;

        int   baseLvl;
        float baseDmg, baseHP;

        bool  open;
        InputAction escA, setA;

    /*────────────────── Awake ──────────────────*/
        void Awake()
        {
            chars   = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            gold    = GameController.SaveManager.GetSave<CurrencySave>(currencyID);
            upgSave = GameController.SaveManager.GetSave<UpgradesSave>("Upgrades Save");

            addGoldBtn.onClick.AddListener(AddGold);
            deleteBtn .onClick.AddListener(DeleteEverything);
            resetBtn  .onClick.AddListener(ResetStats);
            closeBtn  .onClick.AddListener(Close);

            levelSlider.onValueChanged.AddListener(OnLevel);
            dmgSlider  .onValueChanged.AddListener(OnDmg);
            hpSlider   .onValueChanged.AddListener(OnHp);

            gold.onGoldAmountChanged += a => goldLbl.text = $"Current Golds: {a}";
        }

    /*────────────────── helpers ──────────────────*/
        int   GlobalDmgLvl  => GameController.UpgradesManager.GetUpgradeLevel(UpgradeType.Damage);
        float GlobalDmgMult => GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Damage);

        float CalcDamage(int lvl)
        {
            float raw = cData.BaseDamage + CharacterLevelSystem.GetDamageBonus(cData);
            return raw * (GlobalDmgLvl > 0 ? GlobalDmgMult : 1f);
        }
        float CalcHP(int lvl) => cData.BaseHP; // extend if you add HP upgrades

    /*────────────────── Open / Close ──────────────────*/
        public void Open()
        {
            if (open) return;
            open = true;
            gameObject.SetActive(true);

            cData   = charactersDb.GetCharacterData(chars.SelectedCharacterId);
            baseLvl = 1;
            baseDmg = CalcDamage(baseLvl);
            baseHP  = CalcHP(baseLvl);

            int   savedLvl = CharacterLevelSystem.GetLevel(cData);
            float savedDmg = chars.CharacterDamage >= 1f ? chars.CharacterDamage : CalcDamage(savedLvl);
            float savedHP  = chars.CharacterHealth >= 20f ? chars.CharacterHealth : CalcHP(savedLvl);

            levelSlider.Configure(1, 25,  savedLvl, true);
            dmgSlider  .Configure(1f,15f, savedDmg,false);
            hpSlider   .Configure(20f,500f,savedHP,false);

            RefreshValueLabels();
            origLevelLbl.text = $"Original Character Level:   {baseLvl}";
            origDmgLbl  .text = $"Original Character Damage: {baseDmg:0.0}";
            origHpLbl   .text = $"Original Character Health: {baseHP:0}";
            goldLbl.text      = $"Current Golds: {gold.Amount}";

            var map = GameController.InputManager.InputAsset.asset;
            escA = map.FindAction("Back");
            setA = map.FindAction("Settings");
            if (escA != null) escA.performed += OnEsc;
            if (setA != null) setA.performed += OnEsc;

            EventSystem.current.SetSelectedGameObject(closeBtn.gameObject);
        }

        void Close()
        {
            if (!open) return;
            open = false;

            if (escA != null) escA.performed -= OnEsc;
            if (setA != null) setA.performed -= OnEsc;

            GameController.SaveManager.Save(true);
            gameObject.SetActive(false);
        }
        void OnEsc(InputAction.CallbackContext _) => Close();

    /*────────────────── slider callbacks ──────────────────*/
        void OnLevel(float v)
        {
            int lvl = Mathf.Clamp(Mathf.RoundToInt(v), 1, 25);
            levelSlider.SetValueWithoutNotify(lvl);
            CharacterLevelSystem.SetLevel(cData, lvl);

            if (chars.CharacterDamage < 1f) dmgSlider.SetValueWithoutNotify(CalcDamage(lvl));
            if (chars.CharacterHealth < 20f) hpSlider.SetValueWithoutNotify(CalcHP(lvl));

            BroadcastUI();
            RefreshValueLabels();
        }

        void OnDmg(float v)
        {
            float dmg = Mathf.Clamp(Mathf.Round(v * 10f) * 0.1f, 1f, 15f);
            dmgSlider.SetValueWithoutNotify(dmg);
            chars.CharacterDamage = dmg;
            BroadcastUI();
            RefreshValueLabels();
        }

        void OnHp(float v)
        {
            float hp = Mathf.Clamp(Mathf.Round(v / 5f) * 5f, 20f, 500f);
            hpSlider.SetValueWithoutNotify(hp);
            chars.CharacterHealth = hp;
            BroadcastUI();
            RefreshValueLabels();
        }

        void RefreshValueLabels()
        {
            levelValLbl.text = $"Character Level:   {levelSlider.value:0}";
            dmgValLbl  .text = $"Character Damage: {dmgSlider.value:0.0}";
            hpValLbl   .text = $"Character Health: {hpSlider.value:0}";
        }

    /*────────────────── Buttons ──────────────────*/
        void AddGold() => gold.Deposit(1000);

        void DeleteEverything()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            chars.ResetAll();
            gold.ResetAll();
            upgSave?.ResetAll();
            GameController.SaveManager.GetSave<CharacterLevelSave>("CharacterLevels")?.ResetAll();
            GameController.SaveManager.GetSave<StageSave>("Stage")?.ResetAll();
            GameController.SaveManager.GetSave<CurrencySave>("Reroll")?.ResetAll();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            var dir = Application.persistentDataPath;
            if (Directory.Exists(dir))
                foreach (var f in Directory.GetFiles(dir)) File.Delete(f);

            GameController.SaveManager.Save(true);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void ResetStats()
        {
            CharacterLevelSystem.SetLevel(cData, baseLvl);
            chars.CharacterDamage = 0f;
            chars.CharacterHealth = 0f;

            levelSlider.SetValueWithoutNotify(baseLvl);
            dmgSlider  .SetValueWithoutNotify(baseDmg);
            hpSlider   .SetValueWithoutNotify(baseHP);

            BroadcastUI();
            RefreshValueLabels();
        }

    /*────────────────── helpers ──────────────────*/
        void BroadcastUI()
        {
            foreach (var ui in FindObjectsOfType<SelectedCharacterUIBehavior>())
                ui.Setup(cData, abilitiesDb);

            foreach (var evo in FindObjectsOfType<EvoAbilitiesDisplayBehavior>())
                evo.RefreshDisplay();
        }
    }

    /*──── slider helper ────*/
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
