using OctoberStudio.Easing;
using OctoberStudio.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class SettingsWindowBehavior : MonoBehaviour
    {
        [SerializeField] ToggleBehavior soundToggle;
        [SerializeField] ToggleBehavior musicToggle;
        [SerializeField] ToggleBehavior vibrationToggle;
        [SerializeField] ToggleBehavior debugOverlayToggle;

        [Space]
        [SerializeField] GameObject debugOverlayTarget;

        [Space]
        [SerializeField] Button backButton;
        [SerializeField] Button exitButton;

        private void Start()
        {
            EasingManager.DoNextFrame().SetOnFinish(InitToggles);
        }

        private void InitToggles()
        {
            soundToggle.SetToggle(GameController.AudioManager.SoundVolume != 0);
            musicToggle.SetToggle(GameController.AudioManager.MusicVolume != 0);
            vibrationToggle.SetToggle(GameController.VibrationManager.IsVibrationEnabled);
            debugOverlayToggle.SetToggle(debugOverlayTarget.activeSelf);

            soundToggle.onChanged += (enabled) => GameController.AudioManager.SoundVolume = enabled ? 1 : 0;
            musicToggle.onChanged += (enabled) => GameController.AudioManager.MusicVolume = enabled ? 1 : 0;
            vibrationToggle.onChanged += (enabled) => GameController.VibrationManager.IsVibrationEnabled = enabled;
            debugOverlayToggle.onChanged += (enabled) => debugOverlayTarget.SetActive(enabled);
        }

        public void Init(UnityAction onBackButtonClicked)
        {
            backButton.onClick.AddListener(onBackButtonClicked);

#if (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            exitButton.gameObject.SetActive(false);
#else
            exitButton.gameObject.SetActive(true);
            exitButton.onClick.AddListener(OnExitButtonClicked);
#endif
        }

        public void Open()
        {
            gameObject.SetActive(true);
            EasingManager.DoNextFrame(() => {
                soundToggle.Select();
                GameController.InputManager.InputAsset.UI.Back.performed += OnBackInputClicked;
            });
            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        public void Close()
        {
            gameObject.SetActive(false);

            GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        private void OnBackInputClicked(InputAction.CallbackContext context)
        {
            backButton.onClick?.Invoke();
        }

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput == InputType.UIJoystick)
            {
                EasingManager.DoNextFrame(soundToggle.Select);
            }
        }

        private void OnExitButtonClicked()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
