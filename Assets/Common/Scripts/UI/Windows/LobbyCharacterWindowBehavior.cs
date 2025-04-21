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

namespace OctoberStudio.UI.Windows
{
    /// <summary>
    /// Hosts only the Stage selector UI (stage image, name, play button, etc).
    /// Renamed from LobbyWindowBehavior.
    /// </summary>
    public class LobbyCharacterWindowBehavior : MonoBehaviour
    {
        [Header("Stages")]
        [SerializeField] private StagesDatabase stagesDatabase;

        [Space][Header("Stage UI")]
        [SerializeField] private Image    stageIcon;
        [SerializeField] private Image    lockImage;
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private TMP_Text stageNumberLabel;

        [Space][Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        [Space]
        [SerializeField] private Sprite playButtonEnabledSprite;
        [SerializeField] private Sprite playButtonDisabledSprite;

        [Space][Header("Continue Popup")]
        [SerializeField] private Image         continueBackgroundImage;
        [SerializeField] private RectTransform continuePopupRect;
        [SerializeField] private Button        confirmButton;
        [SerializeField] private Button        cancelButton;

        private StageSave stageSave;

        private void Awake()
        {
            playButton   .onClick.AddListener(OnPlayButtonClicked);
            leftButton   .onClick.AddListener(DecrementSelectedStageId);
            rightButton  .onClick.AddListener(IncrementSelectedStageId);
            confirmButton.onClick.AddListener(ConfirmButtonClicked);
            cancelButton .onClick.AddListener(CancelButtonClicked);
        }

        private void Start()
        {
            stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
            stageSave.onSelectedStageChanged += InitStage;

            if (stageSave.IsPlaying && GameController.FirstTimeLoaded)
            {
                continueBackgroundImage.gameObject.SetActive(true);
                continuePopupRect.gameObject.SetActive(true);
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
                InitStage(stageSave.SelectedStageId);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
                stageSave.SetSelectedStageId(stageSave.MaxReachedStageId);
            }

            GameController.InputManager.onInputChanged += OnInputChanged;
            GameController.InputManager.InputAsset.UI.Settings.performed += OnSettingsInputClicked;
        }

        /// <summary>
        /// Hooks up the Upgrades & Settings callbacks from your MainMenu behavior.
        /// </summary>
        public void Init(UnityAction onUpgrades, UnityAction onSettings)
        {
            upgradesButton.onClick.AddListener(onUpgrades);
            settingsButton.onClick.AddListener(onSettings);
        }

        private void InitStage(int stageId)
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
            rightButton.gameObject.SetActive(stageId < stagesDatabase.StagesCount - 1);
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
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            GameController.LoadStage();
        }

        private void IncrementSelectedStageId()
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

        private void ConfirmButtonClicked()
        {
            continueBackgroundImage.gameObject.SetActive(false);
            continuePopupRect.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        private void CancelButtonClicked()
        {
            continueBackgroundImage.gameObject.SetActive(false);
            continuePopupRect.gameObject.SetActive(false);
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            GameController.LoadMainMenu();
        }

        private void OnSettingsInputClicked(InputAction.CallbackContext _) =>
            settingsButton.onClick?.Invoke();

        private void OnInputChanged(InputType prev, InputType curr)
        {
            if (prev == InputType.UIJoystick)
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        private void FallbackSelect(GameObject prefer, GameObject fallback, GameObject secondary)
        {
            if (EventSystem.current.currentSelectedGameObject == prefer)
                EventSystem.current.SetSelectedGameObject(fallback);
            else
                EventSystem.current.SetSelectedGameObject(secondary);
        }
    }
}
