using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using OctoberStudio;
using OctoberStudio.Abilities;
using OctoberStudio.Audio;
using OctoberStudio.Save;
using OctoberStudio.UI;
using OctoberStudio.UI.Windows;
using OctoberStudio.Upgrades.UI;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    /// <summary>
    /// Hosts the character header display and opens the CharactersWindow.
    /// </summary>
    public class MainMenuCharacterBehavior : MonoBehaviour
    {
        [Header("Character Header")]
        [SerializeField] private SelectedCharacterItemBehavior selectedDisplay;
        [SerializeField] private CharactersDatabase           charactersDatabase;
        [SerializeField] private AbilitiesDatabase            abilitiesDatabase;

        [Header("Characters Window")]
        [SerializeField] private CharactersWindowBehavior charactersWindow;
        [SerializeField] private Button                     charactersButton;

        private CharactersSave charactersSave;

        private void Awake()
        {
            charactersButton.onClick.AddListener(ShowCharacters);
        }

        private void Start()
        {
            // Subscribe to save changes
            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            charactersSave.onSelectedCharacterChanged += UpdateSelectedDisplay;
            UpdateSelectedDisplay();

            // Initialize the window’s back callback
            charactersWindow.Init(HideCharacters);
        }

        private void OnDestroy()
        {
            charactersSave.onSelectedCharacterChanged -= UpdateSelectedDisplay;
            charactersButton.onClick.RemoveListener(ShowCharacters);
        }

        private void UpdateSelectedDisplay()
        {
            if (selectedDisplay == null || charactersDatabase == null || abilitiesDatabase == null)
                return;

            var data = charactersDatabase.GetCharacterData(charactersSave.SelectedCharacterId);
            selectedDisplay.Setup(data, abilitiesDatabase);
        }

        private void ShowCharacters()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            charactersWindow.Open();
        }

        private void HideCharacters()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            charactersWindow.Close();
            // Return focus to your header button
            EventSystem.current.SetSelectedGameObject(charactersButton.gameObject);
        }
    }
}
