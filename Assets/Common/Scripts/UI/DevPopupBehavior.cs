using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        [SerializeField] private CharacterLevelingConfig levelingConfig;

        [Header("Currency key (must exist in DB)")]
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
        CharactersSave     chars;
        CurrencySave       gold;
        UpgradesSave       upgSave;
        CharacterData      cData;

        int    baseLvl;
        float  baseDmg, baseHP;

        bool   open;
        bool   isInitializing;
        InputAction escA, setA;

        // Centralize save key
        private const string CharacterLevelsSaveKey = "CharacterLevels";

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

        /*───────── helpers ─────────*/
        float CalcDamage(int lvl)
        {
            float raw     = cData.BaseDamage + CharacterLevelSystem.GetDamageBonus(cData);
            float shopBon = GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Damage);
            return raw + shopBon;
        }

        float CalcHP(int lvl)
        {
            float baseHP  = cData.BaseHP;
            float shopBon = GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Health);
            return baseHP + shopBon;
        }

        /*────────────────── Open / Close ──────────────────*/
        public void Open()
        {
            if (open) return;
            open = true;
            gameObject.SetActive(true);

            isInitializing = true;

            // Load character and compute base stats
            cData   = charactersDb.GetCharacterData(chars.SelectedCharacterId);
            baseLvl = CharacterLevelSystem.GetLevel(cData);
            baseDmg = CalcDamage(baseLvl);
            baseHP  = CalcHP(baseLvl);

            // Dev overrides fallback
            int   savedLvl = baseLvl;
            float savedDmg = chars.CharacterDamage >= 1f   ? chars.CharacterDamage : baseDmg;
            float savedHP  = chars.CharacterHealth >= baseHP ? chars.CharacterHealth  : baseHP;

            // Configure sliders without triggering events
            levelSlider.Configure(1, 25, savedLvl, true);
            dmgSlider  .Configure(1f, 15f, savedDmg, false);
            hpSlider   .Configure(baseHP, 500f, savedHP, false);

            // Update labels
            RefreshValueLabels();
            origLevelLbl.text = $"Original Character Level:   {baseLvl}";
            origDmgLbl  .text = $"Original Character Damage: {baseDmg:0.0}";
            origHpLbl   .text = $"Original Character HP:     {baseHP}";
            goldLbl.text      = $"Current Golds:            {gold.Amount}";

            // Hook ESC / Settings
            var map = GameController.InputManager.InputAsset.asset;
            escA = map.FindAction("Back");
            setA = map.FindAction("Settings");
            if (escA != null) escA.performed += OnEsc;
            if (setA != null) setA.performed += OnEsc;

            EventSystem.current.SetSelectedGameObject(closeBtn.gameObject);

            isInitializing = false;
        }

        void Close()
        {
            if (!open) return;
            open = false;

            if (escA != null) escA.performed -= OnEsc;
            if (setA != null) setA.performed -= OnEsc;

            // Simply close without saving accidental input
            gameObject.SetActive(false);
        }

        void OnEsc(InputAction.CallbackContext _) => Close();

        /*──────────────── slider callbacks ─────────────────*/
        void DumpLevelSaveFields(CharacterLevelSave lvlSave)
        {
            var t = typeof(CharacterLevelSave);
            var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
                Debug.Log($"[DevPopup] Field: {f.Name}");
        }
        void OnLevel(float v)
        {
            // clamp slider to valid range
            int lvl = Mathf.Clamp(Mathf.RoundToInt(v), 1, levelingConfig.MaxLevel);
            levelSlider.SetValueWithoutNotify(lvl);

            // 1) update the logical level
            CharacterLevelSystem.SetLevel(cData, lvl);

            // 2) cheat the underlying XP so the runtime will keep you at this level
            var lvlSave = GameController.SaveManager.GetSave<CharacterLevelSave>(CharacterLevelsSaveKey);

            // find or create the entry for this character
            var entry = lvlSave.Entries.Find(e => e.name == cData.Name);
            if (entry == null)
            {
                entry = new CharacterLevelEntry { name = cData.Name };
                lvlSave.Entries.Add(entry);
            }

            // set XP exactly to the cumulative XP needed for 'lvl'
            entry.xp  = levelingConfig.GetCumulativeXpForLevel(lvl);
            entry.lvl = lvl;

            // persist changes
            lvlSave.Flush();
            GameController.SaveManager.Save(true);

            // 3) recompute damage/HP if no manual overrides
            if (Mathf.Approximately(chars.CharacterDamage, 0f))
                dmgSlider.SetValueWithoutNotify(CalcDamage(lvl));
            if (Mathf.Approximately(chars.CharacterHealth, 0f))
            {
                float newHP = CalcHP(lvl);
                hpSlider.minValue = newHP;
                hpSlider.SetValueWithoutNotify(newHP);
            }

            // 4) update UI
            BroadcastUI();
            RefreshValueLabels();
        }


        void OnDmg(float v)
        {
            if (isInitializing) return;

            float dmg = Mathf.Clamp(Mathf.Round(v * 10f) * 0.1f, 1f, 15f);
            dmgSlider.SetValueWithoutNotify(dmg);
            chars.CharacterDamage = dmg;

            BroadcastUI();
            RefreshValueLabels();
        }

        void OnHp(float v)
        {
            if (isInitializing) return;

            float hp = Mathf.Clamp(Mathf.Round(v / 5f) * 5f, baseHP, 500f);
            hpSlider.minValue = baseHP;
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

        /*──────────────── Buttons ─────────────────*/
        void AddGold() => gold.Deposit(1_000);

        void DeleteEverything()
{
    GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

    // 1) Characters, Gold, Upgrades, Level
    chars.ResetAll();
    gold.ResetAll();
    upgSave?.ResetAll();

    var lvlSave = GameController.SaveManager.GetSave<CharacterLevelSave>(CharacterLevelsSaveKey);
    if (lvlSave != null)
    {
        lvlSave.ResetAll();
        lvlSave.Flush();
    }

    // 2) StageSave
    var stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
    if (stageSave != null)
    {
        stageSave.ResetAll();
        stageSave.Flush();
    }

    // 3) CharacterKillSave – clear disk & memory
    var killSave = GameController.SaveManager.GetSave<CharacterKillSave>("CharacterKillSave");
    if (killSave != null)
    {
        killSave.FromDictionary(new Dictionary<string, int>());  // clear entries
        killSave.Flush();
    }
    // clear static in-memory kills dictionary
    var killsField = typeof(CharacterKillSystem)
        .GetField("kills", BindingFlags.NonPublic | BindingFlags.Static);
    if (killsField != null)
    {
        var dict = killsField.GetValue(null) as System.Collections.IDictionary;
        dict?.Clear();
    }

    // 4) CharacterPlaytimeSystem – clear entries
    var saveMgr = GameController.SaveManager;
    var ptType = typeof(CharacterPlaytimeSystem)
        .GetNestedType("PlaytimeSave", BindingFlags.NonPublic);
    if (ptType != null)
    {
        var getSave = saveMgr.GetType()
            .GetMethod("GetSave", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null)
            .MakeGenericMethod(ptType);
        var ptSave = getSave.Invoke(saveMgr, new object[] { "CharacterPlaytimes" });
        if (ptSave != null)
        {
            var entriesField = ptType.GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = entriesField.GetValue(ptSave) as System.Collections.IList;
            list?.Clear();
            ptType.GetMethod("Flush", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)
                  .Invoke(ptSave, null);
        }
    }

    // 5) Reroll charges
    GameController.SaveManager.GetSave<CurrencySave>("Reroll")?.ResetAll();

    // 6) Delete all PlayerPrefs & raw files
    PlayerPrefs.DeleteAll();
    PlayerPrefs.Save();
    var dir = Application.persistentDataPath;
    if (System.IO.Directory.Exists(dir))
        foreach (var f in System.IO.Directory.GetFiles(dir))
            System.IO.File.Delete(f);

    // 7) Persist cleared state & reload scene
    GameController.SaveManager.Save(true);
    UnityEngine.SceneManagement.SceneManager.LoadScene(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
    );
}









        void ResetStats()
        {
            // Restore base values
            CharacterLevelSystem.SetLevel(cData, baseLvl);
            chars.CharacterDamage = 0f;
            chars.CharacterHealth = 0f;

            levelSlider.SetValueWithoutNotify(baseLvl);
            dmgSlider  .SetValueWithoutNotify(baseDmg);
            hpSlider.minValue = baseHP;
            hpSlider.SetValueWithoutNotify(baseHP);

            BroadcastUI();
            RefreshValueLabels();
        }

        void BroadcastUI()
        {
            foreach (var ui in FindObjectsOfType<SelectedCharacterUIBehavior>())
                ui.Setup(cData, abilitiesDb);
            foreach (var evo in FindObjectsOfType<EvoAbilitiesDisplayBehavior>())
                evo.RefreshDisplay();
        }
    }

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
