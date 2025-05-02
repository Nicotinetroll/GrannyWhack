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
    public class DevPopupBehavior : MonoBehaviour
    {
        /* ---------- Inspector ---------- */
        [Header("Databases / Saves")]
        [SerializeField] private CharactersDatabase charactersDatabase;   // nastav v Inspectore

        [Header("UI – Close & Gold")]
        [SerializeField] private Button   closeButton;
        [SerializeField] private TMP_Text currentGoldLabel;
        [SerializeField] private Button   addGoldButton;
        [SerializeField] private Button   deleteSavesButton;
        [SerializeField] private Button   resetButton;

        [Header("Sliders")]
        [SerializeField] private Slider   levelSlider;   // int, 1‑25
        [SerializeField] private Slider   dmgSlider;     // float, 1‑10
        [SerializeField] private Slider   hpSlider;      // float, 20‑250

        [Header("Original Labels")]
        [SerializeField] private TMP_Text origLevelLabel;
        [SerializeField] private TMP_Text origDmgLabel;
        [SerializeField] private TMP_Text origHpLabel;

        /* ---------- runtime ---------- */
        private CharactersSave charactersSave;
        private CurrencySave   currencySave;

        private CharacterData  selChar;

        private int   startLvl;
        private float startDmg;
        private float startHp;

        private bool popupActive;

        private InputAction backAction;
        private InputAction settingsAction;

        /* ================================================================== */
        private void Awake()
        {
            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            currencySave   = GameController.SaveManager.GetSave<CurrencySave>("Currency");

            if (!EnsureSaves()) return;

            closeButton      .onClick.AddListener(Close);
            addGoldButton    .onClick.AddListener(AddGold);
            deleteSavesButton.onClick.AddListener(DeleteAllSaves);
            resetButton      .onClick.AddListener(ResetAll);

            levelSlider.onValueChanged.AddListener(OnLevelChange);
            dmgSlider  .onValueChanged.AddListener(OnDmgChange);
            hpSlider   .onValueChanged.AddListener(OnHpChange);

            currencySave.onGoldAmountChanged += UpdateGold;
        }

        /* ------------------------ Open / Close ------------------------- */
        public void Open()
        {
            if (!EnsureSaves()) return;

            if (popupActive) return;
            popupActive = true;

            /* ---- načítaj postavu & pôvodné hodnoty ---- */
            selChar   = charactersDatabase.GetCharacterData(charactersSave.SelectedCharacterId);
            startLvl  = CharacterLevelSystem.GetLevel(selChar);
            startDmg  = charactersSave.CharacterDamage;
            startHp   = charactersSave.CharacterHealth;

            levelSlider.SetValueWithoutNotify(startLvl);
            dmgSlider  .SetValueWithoutNotify(startDmg);
            hpSlider   .SetValueWithoutNotify(startHp);

            origLevelLabel.text = $"Original Character Level:   {startLvl}";
            origDmgLabel  .text = $"Original Character Damage: {startDmg}";
            origHpLabel   .text = $"Original Character Health: {startHp}";

            UpdateGold(currencySave.Amount);

            /* ---- input actions „Back“ & „Settings“ ---- */
            var inputAsset = GameController.InputManager.InputAsset.asset;
            backAction     = inputAsset.FindAction("Back");
            settingsAction = inputAsset.FindAction("Settings");

            if (backAction != null)     backAction.performed     += OnEsc;
            if (settingsAction != null) settingsAction.performed += OnEsc;

            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        private void Close()
        {
            if (!popupActive) return;
            popupActive = false;

            if (backAction    != null) backAction   .performed -= OnEsc;
            if (settingsAction!= null) settingsAction.performed -= OnEsc;

            /* SaveManager flush – každý save má vlastný Flush() */
            GameController.SaveManager.GetSave<CharacterLevelSave>("CharacterLevels")?.Flush();
            charactersSave.Flush();
            currencySave  .Flush();

            gameObject.SetActive(false);
        }

        private void OnEsc(InputAction.CallbackContext _) => Close();

        /* ------------------- Slider Callbacks ------------------- */
        private void OnLevelChange(float v)
        {
            int lvl = Mathf.Clamp(Mathf.RoundToInt(v), 1, CharacterLevelSystem.MaxLevel);
            levelSlider.SetValueWithoutNotify(lvl);
            CharacterLevelSystem.SetLevel(selChar, lvl);
        }

        private void OnDmgChange(float v)
        {
            v = Mathf.Clamp(Mathf.Round(v * 10f) * 0.1f, 1f, 10f);
            dmgSlider.SetValueWithoutNotify(v);
            charactersSave.CharacterDamage = v;
        }

        private void OnHpChange(float v)
        {
            v = Mathf.Clamp(Mathf.Round(v), 20f, 250f);
            hpSlider.SetValueWithoutNotify(v);
            charactersSave.CharacterHealth = v;
        }

        /* ------------------- Buttons ------------------- */
        private void AddGold()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            currencySave.Deposit(1000);
        }

        private void DeleteAllSaves()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            string path = Path.Combine(Application.persistentDataPath, "save.dat");
            if (File.Exists(path)) File.Delete(path);

            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void ResetAll()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            CharacterLevelSystem.SetLevel(selChar, startLvl);
            charactersSave.CharacterDamage = startDmg;
            charactersSave.CharacterHealth = startHp;

            levelSlider.SetValueWithoutNotify(startLvl);
            dmgSlider  .SetValueWithoutNotify(startDmg);
            hpSlider   .SetValueWithoutNotify(startHp);
        }

        /* ------------------- Gold label helper ------------------- */
        private void UpdateGold(int amt) =>
            currentGoldLabel.text = $"Current Golds: {amt}";

        /* ------------------- Cleanup ------------------- */
        private void OnDestroy()
        {
            if (backAction    != null) backAction   .performed -= OnEsc;
            if (settingsAction!= null) settingsAction.performed -= OnEsc;
            currencySave.onGoldAmountChanged -= UpdateGold;
        }
        
        bool EnsureSaves()
        {
            if (charactersSave == null)
                charactersSave = GameController.SaveManager?.GetSave<CharactersSave>("Characters");

            if (currencySave == null)
                currencySave   = GameController.SaveManager?.GetSave<CurrencySave>("Currency");

            if (charactersSave == null || currencySave == null)
            {
                Debug.LogError("[DevPopup] Save files not initialised – popup aborted.");
                return false;
            }
            return true;
        }

    }
}
