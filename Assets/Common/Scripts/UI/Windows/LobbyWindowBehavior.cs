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
using OctoberStudio.Extensions;
using OctoberStudio.UI;
using System.IO;

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

        /* -------- NOVÉ: DEV -------- */
        [SerializeField] private Button          devButton;
        [SerializeField] private DevPopupBehavior devPopup;
        /* --------------------------- */

        [Space]
        [SerializeField] private Sprite playButtonEnabledSprite;
        [SerializeField] private Sprite playButtonDisabledSprite;

        [Space, Header("Continue Popup")]
        [SerializeField] private Image         continueBackgroundImage;
        [SerializeField] private RectTransform continuePopupRect;
        [SerializeField] private Button        confirmButton;
        [SerializeField] private Button        cancelButton;

        /* =========================== STAGE POPUP ============================ */
        [Space, Header("Stage Popup")]
        [SerializeField] private Button        stagePopupOpenButton;
        [SerializeField] private Image         stagePopupBG;
        [SerializeField] private RectTransform stagePopupRect;
        [SerializeField] private Button        stagePopupCloseButton;
        /* =================================================================== */

        private StageSave      stageSave;
        private CharactersSave charactersSave;

        private bool stagePopupActive;
        private InputAction backAction;        // Back = Esc/B button
        private InputAction settingsAction;    // Settings = Esc (v menu)

        /* ========================= ESC handler ============================ */
        private void OnBackPopup(InputAction.CallbackContext _) => CloseStagePopup();
        /* ================================================================= */

        /* ------------------------------------------------------------------ */
        private void Awake()
        {
            /* pôvodné listenery */
            playButton   .onClick.AddListener(OnPlayButtonClicked);
            leftButton   .onClick.AddListener(DecrementSelectedStageId);
            rightButton  .onClick.AddListener(IncremenSelectedStageId);
            confirmButton.onClick.AddListener(ConfirmButtonClicked);
            cancelButton .onClick.AddListener(CancelButtonClicked);

            /* StagePopup */
            if (stagePopupOpenButton)   stagePopupOpenButton .onClick.AddListener(OpenStagePopup);
            if (stagePopupCloseButton)  stagePopupCloseButton.onClick.AddListener(CloseStagePopup);

            /* DEV button */
            if (devButton && devPopup) devButton.onClick.AddListener(() => devPopup.Open());
        }

        private void Start()
        {
            /* Stage save */
            stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
            stageSave.onSelectedStageChanged += InitStage;

            /* Characters save & header */
            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            charactersSave.onSelectedCharacterChanged += UpdateSelectedDisplay;
            UpdateSelectedDisplay();

            /* Continue popup or default */
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

            /* StagePopup OFF */
            if (stagePopupBG)   stagePopupBG.gameObject  .SetActive(false);
            if (stagePopupRect) stagePopupRect.gameObject.SetActive(false);

            /* Input hook pre zmenu zariadenia */
            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        /* ===================== STAGE POPUP ====================== */
        private void OpenStagePopup()
        {
            if (stagePopupActive) return;
            stagePopupActive = true;

            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            stagePopupBG  ?.gameObject.SetActive(true);
            stagePopupRect?.gameObject.SetActive(true);

            if (stagePopupCloseButton)
                EventSystem.current.SetSelectedGameObject(stagePopupCloseButton.gameObject);

            /* nájdeme akcie z InputActionAsset */
            var asset = GameController.InputManager.InputAsset.asset;
            backAction     = asset.FindAction("Back");
            settingsAction = asset.FindAction("Settings");

            if (backAction != null)     backAction.performed     += OnBackPopup;
            if (settingsAction != null) settingsAction.performed += OnBackPopup;
        }

        private void CloseStagePopup()
        {
            if (!stagePopupActive) return;
            stagePopupActive = false;

            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            if (stagePopupBG)   stagePopupBG  .gameObject.SetActive(false);
            if (stagePopupRect) stagePopupRect.gameObject.SetActive(false);

            if (backAction != null)     backAction.performed     -= OnBackPopup;
            if (settingsAction != null) settingsAction.performed -= OnBackPopup;

            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        /* =========================== ZVYŠOK =========================== */

        private void UpdateSelectedDisplay(int _ = 0)
        {
            if (selectedDisplay == null
                || charactersDatabase == null
                || abilitiesDatabase == null
                || charactersSave == null)
                return;

            var data = charactersDatabase.GetCharacterData(charactersSave.SelectedCharacterId);
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

            stageLabel      .text = stage.DisplayName;
            stageNumberLabel.text = $"Stage {stageId + 1}";
            stageIcon       .sprite = stage.Icon;

            stageDescriptionLabel?.SetText(stage.Description);
            stageRecDamageLabel ?.SetText($"{stage.RecommendedDamage:N0}");
            stageRecHealthLabel ?.SetText($"{stage.RecommendedHealth:N0}");

            bool locked = stageId > stageSave.MaxReachedStageId;
            lockImage.gameObject.SetActive(locked);

            playButton.interactable = !locked;
            playButton.image.sprite = locked ? playButtonDisabledSprite : playButtonEnabledSprite;

            leftButton .gameObject.SetActive(!stageSave.IsFirstStageSelected);
            rightButton.gameObject.SetActive(stageId != stagesDatabase.StagesCount - 1);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            EasingManager.DoNextFrame(() =>
                EventSystem.current.SetSelectedGameObject(playButton.gameObject));

            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        public void Close()
        {
            gameObject.SetActive(false);
            GameController.InputManager.onInputChanged -= OnInputChanged;

            if (backAction != null)     backAction.performed     -= OnBackPopup;
            if (settingsAction != null) settingsAction.performed -= OnBackPopup;
        }

        /* ------------------- Buttons ------------------- */
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
            if (primary.activeSelf)      EventSystem.current.SetSelectedGameObject(primary);
            else if (secondary.activeSelf) EventSystem.current.SetSelectedGameObject(secondary);
            else                          EventSystem.current.SetSelectedGameObject(tertiary);
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
                .SetOnFinish(() => continueBackgroundImage.gameObject.SetActive(false));

            continuePopupRect
                .DoAnchorPosition(Vector2.down * 2500, 0.3f)
                .SetEasing(EasingType.SineIn)
                .SetOnFinish(() => continuePopupRect.gameObject.SetActive(false));

            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        private void OnSettingsInputClicked(InputAction.CallbackContext _)
        {
            settingsButton.onClick?.Invoke();
        }

        private void OnInputChanged(InputType prev, InputType current)
        {
            if (prev != InputType.UIJoystick) return;

            if (stagePopupActive)
            {
                EventSystem.current.SetSelectedGameObject(stagePopupCloseButton.gameObject);
                return;
            }

            var toSelect = continueBackgroundImage.gameObject.activeSelf
                ? confirmButton.gameObject
                : playButton.gameObject;
            EventSystem.current.SetSelectedGameObject(toSelect);
        }

        private void OnDestroy()
        {
            stageSave.onSelectedStageChanged -= InitStage;
            GameController.InputManager.onInputChanged -= OnInputChanged;
            if (backAction != null)     backAction.performed     -= OnBackPopup;
            if (settingsAction != null) settingsAction.performed -= OnBackPopup;
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= UpdateSelectedDisplay;
        }
    }
}
