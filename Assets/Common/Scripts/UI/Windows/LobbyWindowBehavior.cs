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
using OctoberStudio.UI;

namespace OctoberStudio.UI
{
    public class LobbyWindowBehavior : MonoBehaviour
    {
        [Header("Header Display")]
        [SerializeField] private SelectedCharacterItemBehavior selectedDisplay;
        [SerializeField] private CharactersDatabase           charactersDatabase;
        [SerializeField] private AbilitiesDatabase            abilitiesDatabase;

        [Header("Stages")]
        [SerializeField] private StagesDatabase stagesDatabase;

        [Space] [Header("Stage UI")]
        [SerializeField] private Image    stageIcon;
        [SerializeField] private Image    lockImage;
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private TMP_Text stageNumberLabel;

        [Space] [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button charactersButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        [Space] 
        [SerializeField] private Sprite playButtonEnabledSprite;
        [SerializeField] private Sprite playButtonDisabledSprite;

        [Space] [Header("Continue Popup")]
        [SerializeField] private Image         continueBackgroundImage;
        [SerializeField] private RectTransform continuePopupRect;
        [SerializeField] private Button        confirmButton;
        [SerializeField] private Button        cancelButton;

        private StageSave      stageSave;
        private CharactersSave charactersSave;

        private void Awake()
        {
            playButton   .onClick.AddListener(OnPlayButtonClicked);
            leftButton   .onClick.AddListener(DecrementSelectedStageId);
            rightButton  .onClick.AddListener(IncremenSelectedStageId);
            confirmButton.onClick.AddListener(ConfirmButtonClicked);
            cancelButton .onClick.AddListener(CancelButtonClicked);
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
                continuePopupRect       .gameObject.SetActive(true);
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
                InitStage(stageSave.SelectedStageId);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
                stageSave.SetSelectedStageId(stageSave.MaxReachedStageId);
            }

            // Input hooks
            GameController.InputManager.onInputChanged += OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed += OnSettingsInputClicked;
        }

        private void UpdateSelectedDisplay()
        {
            if (selectedDisplay == null
             || charactersDatabase == null
             || abilitiesDatabase == null
             || charactersSave == null)
                return;

            var data = charactersDatabase.GetCharacterData(
                charactersSave.SelectedCharacterId);

            // **Pass BOTH databases into Setup**
            selectedDisplay.Setup(data, abilitiesDatabase);
        }

        public void Init(UnityAction onUpgrades, UnityAction onSettings, UnityAction onCharacters)
        {
            upgradesButton .onClick.AddListener(onUpgrades);
            settingsButton .onClick.AddListener(onSettings);
            charactersButton.onClick.AddListener(onCharacters);
        }

        public void InitStage(int stageId)
        {
            var stage = stagesDatabase.GetStage(stageId);
            stageLabel      .text   = stage.DisplayName;
            stageNumberLabel.text   = $"Stage {stageId + 1}";
            stageIcon       .sprite = stage.Icon;

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
                var toSelect = continueBackgroundImage.gameObject.activeSelf
                    ? confirmButton.gameObject
                    : playButton.gameObject;
                EventSystem.current.SetSelectedGameObject(toSelect);
            }
        }

        private void OnDestroy()
        {
            stageSave.onSelectedStageChanged        -= InitStage;
            GameController.InputManager.onInputChanged -= OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed -= OnSettingsInputClicked;
            if (charactersSave != null)
                charactersSave.onSelectedCharacterChanged -= UpdateSelectedDisplay;
        }
    }
}
