using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using OctoberStudio.Audio;
using OctoberStudio.Input;
using OctoberStudio.Save;

namespace OctoberStudio.UI
{
    /// <summary>
    /// Developer‑only popup na úpravu charakter levelu, dmg, HP a goldov.
    /// </summary>
    public sealed class DevPopupBehavior : MonoBehaviour
    {
        /* ────────────── Inspector references ────────────── */
        [Header("Databases")]
        [SerializeField] CharactersDatabase charactersDatabase;

        [Header("Buttons")]
        [SerializeField] Button closeBtn;
        [SerializeField] Button addGoldBtn;
        [SerializeField] Button deleteSaveBtn;
        [SerializeField] Button resetBtn;

        [Header("Sliders")]
        [SerializeField] Slider levelSlider;   // int 1‑25
        [SerializeField] Slider dmgSlider;     // float 1‑15
        [SerializeField] Slider hpSlider;      // float 20‑500

        [Header("Value Labels (nad sliderom)")]
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text dmgLabel;
        [SerializeField] TMP_Text hpLabel;

        [Header("Original Labels")]
        [SerializeField] TMP_Text origLevelLabel;
        [SerializeField] TMP_Text origDmgLabel;
        [SerializeField] TMP_Text origHpLabel;

        [Header("Gold Label")]
        [SerializeField] TMP_Text goldLabel;

        /* ────────────── runtime ────────────── */
        CharactersSave charactersSave;
        CurrencySave   currencySave;

        CharacterData  selChar;

        int   startLvl;
        float startDmg;
        float startHp;

        bool popupActive;

        InputAction backAction;
        InputAction settingsAction;

        /* ─────────────────── Awake ─────────────────── */
        void Awake()
        {
            // získaj savy (SaveManager je initialised od GameController)
            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            currencySave   = GameController.SaveManager.GetSave<CurrencySave>("Currency");

            // hooky na tlačidlá
            closeBtn     .onClick.AddListener(Close);
            addGoldBtn   .onClick.AddListener(AddGold);
            deleteSaveBtn.onClick.AddListener(DeleteAll);
            resetBtn     .onClick.AddListener(ResetAll);

            // hooky na slidery
            levelSlider .onValueChanged.AddListener(OnLevelChanged);
            dmgSlider   .onValueChanged.AddListener(OnDmgChanged);
            hpSlider    .onValueChanged.AddListener(OnHpChanged);

            // gold update
            currencySave.onGoldAmountChanged += UpdateGold;

            // slider nastavovanie limít
            levelSlider.minValue = 1;
            levelSlider.maxValue = CharacterLevelSystem.MaxLevel;
            levelSlider.wholeNumbers = true;

            dmgSlider.minValue = 1f;
            dmgSlider.maxValue = 15f;
            dmgSlider.wholeNumbers = false;

            hpSlider.minValue = 20f;
            hpSlider.maxValue = 500f;
            hpSlider.wholeNumbers = false;
        }

        /* ─────────────────── Public API ─────────────────── */
        public void Open()
        {
            if (popupActive) return;
            popupActive = true;

            gameObject.SetActive(true);

            // načítaj práve vybranú postavu
            selChar  = charactersDatabase.GetCharacterData(charactersSave.SelectedCharacterId);

            // pôvodné hodnoty
            startLvl = CharacterLevelSystem.GetLevel(selChar);
            startDmg = charactersSave.CharacterDamage <= 0f ? selChar.BaseDamage : charactersSave.CharacterDamage;
            startHp  = charactersSave.CharacterHealth <= 0f ? selChar.BaseHP     : charactersSave.CharacterHealth;

            // slider defaulty
            levelSlider.SetValueWithoutNotify(startLvl);
            dmgSlider  .SetValueWithoutNotify(startDmg);
            hpSlider   .SetValueWithoutNotify(startHp);

            UpdateLabels();
            UpdateGold(currencySave.Amount);

            // Input actions ESC / Settings
            var asset = GameController.InputManager.InputAsset.asset;
            backAction     = asset.FindAction("Back");
            settingsAction = asset.FindAction("Settings");
            if (backAction    != null) backAction   .performed += OnEsc;
            if (settingsAction!= null) settingsAction.performed += OnEsc;

            EventSystem.current.SetSelectedGameObject(closeBtn.gameObject);
        }

        /* ─────────────────── Close / ESC ─────────────────── */
        void Close()
        {
            if (!popupActive) return;
            popupActive = false;

            if (backAction    != null) backAction   .performed -= OnEsc;
            if (settingsAction!= null) settingsAction.performed -= OnEsc;

            // flushni savy
            charactersSave.Flush();
            currencySave  .Flush();
            GameController.SaveManager.GetSave<CharacterLevelSave>("CharacterLevels")?.Flush();

            gameObject.SetActive(false);
        }
        void OnEsc(InputAction.CallbackContext _) => Close();

        /* ─────────────────── Slider callbacks ─────────────────── */
        void OnLevelChanged(float v)
        {
            int lvl = Mathf.Clamp(Mathf.RoundToInt(v), 1, CharacterLevelSystem.MaxLevel);
            levelSlider.SetValueWithoutNotify(lvl);
            CharacterLevelSystem.SetLevel(selChar, lvl);
            UpdateLabels();
        }
        void OnDmgChanged(float v)
        {
            float dmg = Mathf.Clamp(Mathf.Round(v * 10f) * 0.1f, 1f, 15f);
            dmgSlider.SetValueWithoutNotify(dmg);
            charactersSave.CharacterDamage = dmg;
            UpdateLabels();
        }
        void OnHpChanged(float v)
        {
            float hp = Mathf.Clamp(Mathf.Round(v / 5f) * 5f, 20f, 500f);
            hpSlider.SetValueWithoutNotify(hp);
            charactersSave.CharacterHealth = hp;
            UpdateLabels();
        }

        /* ─────────────────── Buttons ─────────────────── */
        void AddGold()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            currencySave.Deposit(1000);
        }
        void DeleteAll()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            // PlayerPrefs + súbor save
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            string p = Path.Combine(Application.persistentDataPath, "save.dat");
            if (File.Exists(p)) File.Delete(p);

            // Reload scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
        void ResetAll()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            levelSlider.SetValueWithoutNotify(startLvl);
            dmgSlider  .SetValueWithoutNotify(startDmg);
            hpSlider   .SetValueWithoutNotify(startHp);

            CharacterLevelSystem.SetLevel(selChar, startLvl);
            charactersSave.CharacterDamage = startDmg;
            charactersSave.CharacterHealth = startHp;

            UpdateLabels();
        }

        /* ─────────────────── Helpers ─────────────────── */
        void UpdateLabels()
        {
            levelLabel.text = $"Character Level: {levelSlider.value:0}";
            dmgLabel  .text = $"Character Damage: {dmgSlider.value:0.0}";
            hpLabel   .text = $"Character Health: {hpSlider.value:0}";

            origLevelLabel.text = $"Original Level: {startLvl}";
            origDmgLabel  .text = $"Original Damage: {startDmg:0.0}";
            origHpLabel   .text = $"Original Health: {startHp:0}";
        }
        void UpdateGold(int amt)
        {
            if (goldLabel != null)
                goldLabel.text = $"Current Golds: {amt}";
        }

        /* ─────────────────── Cleanup ─────────────────── */
        void OnDestroy()
        {
            if (backAction    != null) backAction   .performed -= OnEsc;
            if (settingsAction!= null) settingsAction.performed -= OnEsc;
            if (currencySave != null)
                currencySave.onGoldAmountChanged -= UpdateGold;
        }
    }
}
