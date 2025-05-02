using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using OctoberStudio.Save;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using OctoberStudio.Abilities;
using OctoberStudio.Extensions;   // ← kvôli SetAlpha()
using OctoberStudio.UI;

namespace OctoberStudio.UI
{
    public class LobbyWindowBehavior : MonoBehaviour
    {
        /* ============================= ORIGINÁL ============================= */

        [Header("Header Display")]
        [SerializeField] private SelectedCharacterUIBehavior selectedDisplay;
        [SerializeField] private CharactersDatabase           charactersDatabase;
        [SerializeField] private AbilitiesDatabase            abilitiesDatabase;

        [Header("Stages")]
        [SerializeField] private StagesDatabase stagesDatabase;

        [Space, Header("Stage UI")]
        [SerializeField] private Image    stageIcon;
        [SerializeField] private Image    lockImage;
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private TMP_Text stageNumberLabel;
        [SerializeField] private TMP_Text stageDescriptionLabel;
        [SerializeField] private TMP_Text stageRecDamageLabel;
        [SerializeField] private TMP_Text stageRecHealthLabel;

        [Space, Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button charactersButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        [Space]
        [SerializeField] private Sprite playButtonEnabledSprite;
        [SerializeField] private Sprite playButtonDisabledSprite;

        [Space, Header("Continue Popup")]
        [SerializeField] private Image         continueBackgroundImage;
        [SerializeField] private RectTransform continuePopupRect;
        [SerializeField] private Button        confirmButton;
        [SerializeField] private Button        cancelButton;

        /* =========================== NOVÉ HOVNÁ ============================ */
        [Space, Header("Stage Popup")]
        [SerializeField] private Button        stagePopupOpenButton;   // Button_Info
        [SerializeField] private Image         stagePopupBG;           // StagePopupBG
        [SerializeField] private RectTransform stagePopupRect;         // StagePopup panel
        [SerializeField] private Button        stagePopupCloseButton;  // CloseButton
        /* =================================================================== */

        private StageSave      stageSave;
        private CharactersSave charactersSave;
        private bool stagePopupActive;

        /* ========================= ESC  handler ============================ */
        private void OnBackPopup(InputAction.CallbackContext _)
        {
            if (!stagePopupActive) return;    // popup už neexistuje, ignoruj
            CloseStagePopup();
        }
        /* =================================================================== */

        /* ------------------------------------------------------------------- */
        private void Awake()
        {
            /* pôvodné listenery */
            playButton   .onClick.AddListener(OnPlayButtonClicked);
            leftButton   .onClick.AddListener(DecrementSelectedStageId);
            rightButton  .onClick.AddListener(IncremenSelectedStageId);
            confirmButton.onClick.AddListener(ConfirmButtonClicked);
            cancelButton .onClick.AddListener(CancelButtonClicked);

            /* nové listenery – Stage Popup */
            if (stagePopupOpenButton  != null) stagePopupOpenButton .onClick.AddListener(OpenStagePopup);
            if (stagePopupCloseButton != null) stagePopupCloseButton.onClick.AddListener(CloseStagePopup);
        }

        private void Start()
        {
            // Stage save
            stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
            stageSave.onSelectedStageChanged += InitStage;

            // Character save & header display
            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            charactersSave.onSelectedCharacterChanged += UpdateSelectedDisplay;
            UpdateSelectedDisplay();

            // Continue popup or default
            if (stageSave.IsPlaying && GameController.FirstTimeLoaded)
            {
                continueBackgroundImage.gameObject.SetActive(true);
                continuePopupRect.gameObject       .SetActive(true);
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
                InitStage(stageSave.SelectedStageId);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
                stageSave.SetSelectedStageId(stageSave.MaxReachedStageId);
            }

            /* Stage Popup default OFF */
            if (stagePopupBG   != null) stagePopupBG  .gameObject.SetActive(false);
            if (stagePopupRect != null) stagePopupRect.gameObject.SetActive(false);

            /* Input hooks */
            GameController.InputManager.onInputChanged += OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed += OnSettingsInputClicked;
        }

        /* ===================== METÓDY PRE STAGE POPUP ====================== */

        private void OpenStagePopup()
        {
            if (stagePopupActive) return;         // už otvorené → vypadni
            stagePopupActive = true;

            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            stagePopupBG  ?.gameObject.SetActive(true);
            stagePopupRect?.gameObject.SetActive(true);

            if (stagePopupCloseButton != null)
                EventSystem.current.SetSelectedGameObject(stagePopupCloseButton.gameObject);

            // zapíš hooky
            var ui = GameController.InputManager.InputAsset.UI;
            ui.Back.performed     += OnBackPopup;
            ui.Settings.performed += OnBackPopup;
            ui.Settings.performed -= OnSettingsInputClicked;     // vypni pôvodný
        }

        private void CloseStagePopup()
        {
            if (!stagePopupActive) return;            // už zavreté – vypadni
            stagePopupActive = false;                 // zamkni, aby dvojklik nešiel

            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            /* ------------- BEZPEČNÉ VYPÍNANIE ------------- */
            if (stagePopupBG)        // ← Unity "fake null" check
                stagePopupBG.gameObject.SetActive(false);

            if (stagePopupRect)
                stagePopupRect.gameObject.SetActive(false);

            /* ------------- INPUT ODPOJENIE ------------- */
            var ui = GameController.InputManager.InputAsset.UI;
            ui.Back.performed     -= OnBackPopup;
            ui.Settings.performed -= OnBackPopup;
            ui.Settings.performed += OnSettingsInputClicked;

            /* ------------- FOCUS SPAŤ NA PLAY ------------- */
            if (playButton) EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }




        /* =========================== ZVYŠOK KÓDU =========================== */

        private void UpdateSelectedDisplay()
        {
            if (selectedDisplay == null
             || charactersDatabase == null
             || abilitiesDatabase == null
             || charactersSave == null)
                return;

            var data = charactersDatabase.GetCharacterData(
                charactersSave.SelectedCharacterId);

            selectedDisplay.Setup(data, abilitiesDatabase);
        }

        public void Init(UnityAction onUpgrades, UnityAction onSettings, UnityAction onCharacters)
        {
            upgradesButton  .onClick.AddListener(onUpgrades);
            settingsButton  .onClick.AddListener(onSettings);
            charactersButton.onClick.AddListener(onCharacters);
        }

        public void InitStage(int stageId)
        {
            var stage = stagesDatabase.GetStage(stageId);

            stageLabel      .text   = stage.DisplayName;
            stageNumberLabel.text   = $"Stage {stageId + 1}";
            stageIcon       .sprite = stage.Icon;

            /* --------- NOVÉ TEXTY --------- */
            if (stageDescriptionLabel != null)
                stageDescriptionLabel.text = stage.Description;

            if (stageRecDamageLabel != null)
                stageRecDamageLabel.text = $"{stage.RecommendedDamage:N0}";

            if (stageRecHealthLabel != null)
                stageRecHealthLabel.text = $"{stage.RecommendedHealth:N0}";
            /* -------------------------------- */

            bool locked = stageId > stageSave.MaxReachedStageId;
            lockImage.gameObject.SetActive(locked);

            playButton.interactable = !locked;
            playButton.image.sprite = locked
                ? playButtonDisabledSprite
                : playButtonEnabledSprite;

            leftButton .gameObject.SetActive(!stageSave.IsFirstStageSelected);
            rightButton.gameObject.SetActive(stageId != stagesDatabase.StagesCount - 1);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            EasingManager.DoNextFrame(() =>
                EventSystem.current.SetSelectedGameObject(playButton.gameObject));

            GameController.InputManager.onInputChanged += OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed += OnSettingsInputClicked;
        }

        public void Close()
        {
            gameObject.SetActive(false);
            GameController.InputManager.onInputChanged -= OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed -= OnSettingsInputClicked;
            /* istota – odpoj Back keby popup zostal visieť */
            GameController.InputManager.InputAsset.UI.Back.performed -= OnBackPopup;
        }

        private void OnPlayButtonClicked()
        {
            stageSave.IsPlaying      = true;
            stageSave.ResetStageData = true;
            stageSave.Time           = 0f;
            stageSave.XP             = 0f;
            stageSave.XPLEVEL        = 0;

            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            GameController.LoadStage();
        }

        private void IncremenSelectedStageId()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            stageSave.SetSelectedStageId(stageSave.SelectedStageId + 1);
            FallbackSelect(leftButton.gameObject, playButton.gameObject, rightButton.gameObject);
        }

        private void DecrementSelectedStageId()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            stageSave.SetSelectedStageId(stageSave.SelectedStageId - 1);
            FallbackSelect(rightButton.gameObject, playButton.gameObject, leftButton.gameObject);
        }

        private void FallbackSelect(GameObject primary, GameObject secondary, GameObject tertiary)
        {
            if (primary.activeSelf)
                EventSystem.current.SetSelectedGameObject(primary);
            else if (secondary.activeSelf)
                EventSystem.current.SetSelectedGameObject(secondary);
            else
                EventSystem.current.SetSelectedGameObject(tertiary);
        }

        private void ConfirmButtonClicked()
        {
            stageSave.ResetStageData = false;
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            GameController.LoadStage();
        }

        private void CancelButtonClicked()
        {
            stageSave.IsPlaying = false;
            continueBackgroundImage
                .DoAlpha(0, 0.3f)
                .SetOnFinish(() =>
                    continueBackgroundImage.gameObject.SetActive(false));

            continuePopupRect
                .DoAnchorPosition(Vector2.down * 2500, 0.3f)
                .SetEasing(EasingType.SineIn)
                .SetOnFinish(() =>
                    continuePopupRect.gameObject.SetActive(false));

            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        private void OnSettingsInputClicked(InputAction.CallbackContext _)
        {
            settingsButton.onClick?.Invoke();
        }

        private void OnInputChanged(InputType prev, InputType current)
        {
            if (prev == InputType.UIJoystick)
            {
                /* ak je otvorený Stage Popup, drž fokus na ňom */
                if (stagePopupRect != null && stagePopupRect.gameObject.activeSelf)
                {
                    EventSystem.current.SetSelectedGameObject(stagePopupCloseButton.gameObject);
                    return;
                }

                var toSelect = continueBackgroundImage.gameObject.activeSelf
                    ? confirmButton.gameObject
                    : playButton.gameObject;
                EventSystem.current.SetSelectedGameObject(toSelect);
            }
        }

        private void OnDestroy()
        {
            stageSave.onSelectedStageChanged -= InitStage;
            GameController.InputManager.onInputChanged -= OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed -= OnSettingsInputClicked;
            GameController.InputManager.InputAsset.UI.Back.performed     -= OnBackPopup;
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= UpdateSelectedDisplay;
            var ui = GameController.InputManager.InputAsset.UI;
            ui.Back.performed     -= OnBackPopup;
            ui.Settings.performed -= OnBackPopup;
        }
    }
}
