using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Save;
using OctoberStudio.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OctoberStudio.Abilities.UI
{
    public class AbilityCardBehavior : MonoBehaviour
    {
        [Header("Databases")]
        [SerializeField] private CharactersDatabase charactersDatabase;

        [SerializeField] Image abilityIcon;

        [Space]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;

        [Header("Level Text")]
        [SerializeField] TMP_Text levelText;
        [SerializeField] Image levelBackgroundImage;
        [SerializeField] Color levelBackgroundColor;
        [SerializeField] Color levelBackgroundNewColor;
        [SerializeField] Color levelBackgroundEvoColor;

        [Space]
        [SerializeField] GameObject evolutionBlock;
        [SerializeField] Image evolutionIcon;

        [Space]
        [SerializeField] Button button;
        public Selectable Selectable => button;

        [Space]
        [SerializeField] RectTransform shineRect;

        [Header("Icon Background")]
        [SerializeField] Image iconBackgroundImage;
        [SerializeField] Color iconBackgroundColor;
        [SerializeField] Color iconBackgroundEvoColor;

        private Vector2 shineStartPosition;

        public AbilityData Data { get; private set; }

        private System.Action<AbilityData> onAbilitySelected;

        private void Awake()
        {
            button.onClick.AddListener(OnAbilitySelected);
            shineStartPosition = shineRect.anchoredPosition;
        }

        public void Init(System.Action<AbilityData> onAbilitySelected)
        {
            this.onAbilitySelected = onAbilitySelected;
        }

        public void SetData(AbilityData abilityData, int level)
        {
            Data = abilityData;

            abilityIcon.sprite = abilityData.Icon;
            titleText.text = abilityData.Title;
            descriptionText.text = abilityData.Description;

            if (abilityData.IsEvolution)
            {
                levelBackgroundImage.color = levelBackgroundEvoColor;
                levelText.text = $"EVO";
            }
            else if (level == -1 || abilityData.IsEndgameAbility)
            {
                levelBackgroundImage.color = levelBackgroundNewColor;
                levelText.text = $"NEW!";
            }
            else
            {
                levelBackgroundImage.color = levelBackgroundColor;
                levelText.text = $"LVL {level + 2}";
            }

            iconBackgroundImage.color = abilityData.IsEvolution
                ? iconBackgroundEvoColor
                : iconBackgroundColor;

            // ✅ Filter EVOs based on selected character
            if (StageController.AbilityManager.HasEvolution(Data.AbilityType, out var otherType))
            {
                var otherData = StageController.AbilityManager.GetAbilityData(otherType);

                var characterSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
                var selectedCharacterId = characterSave.SelectedCharacterId;
                var characterData = charactersDatabase.GetCharacterData(selectedCharacterId);

                bool isForThisCharacter =
                    !otherData.IsCharacterSpecific ||
                    string.Equals(otherData.AllowedCharacterName,
                                  characterData.Name,
                                  System.StringComparison.OrdinalIgnoreCase);

                if (isForThisCharacter)
                {
                    evolutionBlock.SetActive(true);
                    evolutionIcon.sprite = otherData.Icon;
                }
                else
                {
                    evolutionBlock.SetActive(false);
                }
            }
            else
            {
                evolutionBlock.SetActive(false);
            }
        }

        public void Show(float delay)
        {
            var targetShinePosition = shineStartPosition;
            targetShinePosition.x *= -1;

            shineRect.anchoredPosition = shineStartPosition;
            shineRect.DoAnchorPosition(targetShinePosition, 0.5f, delay).SetUnscaledTime(true);
        }

        private void OnAbilitySelected()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            onAbilitySelected?.Invoke(Data);
        }
    }
}
