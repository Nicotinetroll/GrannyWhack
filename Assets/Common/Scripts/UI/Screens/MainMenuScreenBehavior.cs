using UnityEngine;
using OctoberStudio;        // for GameController & CharactersSave
using OctoberStudio.Audio;  // for AudioManager
using OctoberStudio.UI;
using OctoberStudio.Upgrades.UI; // for SelectedCharacterItemBehavior

namespace OctoberStudio.UI
{
    public class MainMenuScreenBehavior : MonoBehaviour
    {
        [Header("Windows")]
        [SerializeField] private LobbyWindowBehavior      lobbyWindow;
        [SerializeField] private UpgradesWindowBehavior   upgradesWindow;
        [SerializeField] private SettingsWindowBehavior   settingsWindow;
        [SerializeField] private CharactersWindowBehavior charactersWindow;
        

        [Header("Data")]
        [SerializeField] private CharactersDatabase charactersDatabase;

        private void Start()
        {
            // existing window inits
            lobbyWindow.Init(ShowUpgrades, ShowSettings, ShowCharacters);
            upgradesWindow.Init(HideUpgrades);
            settingsWindow.Init(HideSettings);
            charactersWindow.Init(HideCharacters);
        }

        private void ShowUpgrades()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            lobbyWindow.Close();
            upgradesWindow.Open();
        }

        private void HideUpgrades()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            upgradesWindow.Close();
            lobbyWindow.Open();
        }

        private void ShowCharacters()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            lobbyWindow.Close();
            charactersWindow.Open();
        }

        private void HideCharacters()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            charactersWindow.Close();
            lobbyWindow.Open();
        }

        private void ShowSettings()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            lobbyWindow.Close();
            settingsWindow.Open();
        }

        private void HideSettings()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            settingsWindow.Close();
            lobbyWindow.Open();
        }

        private void OnDestroy()
        {
            charactersWindow.Clear();
            upgradesWindow.Clear();
        }
    }
}
