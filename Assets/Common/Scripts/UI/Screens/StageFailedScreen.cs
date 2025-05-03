using System;
using System.Collections.Generic;
using OctoberStudio.Abilities.UI;
using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using OctoberStudio.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class StageFailedScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button reviveButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TextMeshProUGUI totalDamageText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI timeSurvivedText;

        [Header("Abilities List (Same as Pause)")]
        [SerializeField] private List<AbilitiesIndicatorsListBehavior> abilityLists;

        private bool revivedAlready = false;

        private void Awake()
        {
            reviveButton.onClick.AddListener(ReviveButtonClick);
            exitButton.onClick.AddListener(ExitButtonClick);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            totalDamageText.text = PlayerStatsManager.Instance != null
                ? $"{Mathf.RoundToInt(PlayerStatsManager.Instance.TotalDamage):N0}"
                : "0";

            levelText.text = $"{StageController.ExperienceManager.Level + 1}";

            var time = TimeSpan.FromSeconds(GameController.SaveManager.GetSave<StageSave>("Stage").Time);
            timeSurvivedText.text = $"{time:mm\\:ss}";

            foreach (var list in abilityLists)
            {
                if (list == null) continue;
                list.Show();
                list.Refresh();
            }

            canvasGroup.alpha = 0;
            canvasGroup.DoAlpha(1f, 0.3f).SetUnscaledTime(true);

            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Revive) && !revivedAlready)
            {
                reviveButton.gameObject.SetActive(true);
                EventSystem.current.SetSelectedGameObject(reviveButton.gameObject);
            }
            else
            {
                reviveButton.gameObject.SetActive(false);
                EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
            }

            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        public void Hide(UnityAction onFinish)
        {
            foreach (var list in abilityLists)
                list?.Hide();

            canvasGroup.DoAlpha(0f, 0.3f).SetUnscaledTime(true).SetOnFinish(() =>
            {
                gameObject.SetActive(false);
                onFinish?.Invoke();
            });

            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        private void ReviveButtonClick()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            Hide(StageController.ResurrectPlayer);
            revivedAlready = true;
        }

        private void ExitButtonClick()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            Time.timeScale = 1f;
            StageController.ReturnToMainMenu();
            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            EventSystem.current.SetSelectedGameObject(
                GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Revive) && !revivedAlready
                    ? reviveButton.gameObject
                    : exitButton.gameObject
            );
        }
    }
}
