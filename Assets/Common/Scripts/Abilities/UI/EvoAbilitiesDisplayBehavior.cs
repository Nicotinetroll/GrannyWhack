using UnityEngine;
using OctoberStudio.Abilities;
using OctoberStudio.Save;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class EvoAbilitiesDisplayBehavior : MonoBehaviour
    {
        [Header("Data & Prefabs")]
        [Tooltip("Your CharactersDatabase ScriptableObject")]
        [SerializeField] private CharactersDatabase charactersDatabase;
        [Tooltip("Your AbilitiesDatabase ScriptableObject")]
        [SerializeField] private AbilitiesDatabase  abilitiesDatabase;
        [Tooltip("Optional: leave blank to auto‑fetch from SaveManager")]
        [SerializeField] private CharactersSave      characterSave;
        [Tooltip("One generic Evo‑item prefab with EvoAbilityItemBehavior on it")]
        [SerializeField] private GameObject          evoItemPrefab;

        [Header("Layout")]
        [Tooltip("The RectTransform under your Evo Upgrades HorizontalLayoutGroup")]
        [SerializeField] private RectTransform       container;

        private void Start()
        {
            // grab save if not set
            if (characterSave == null)
                characterSave = GameController
                                   .SaveManager
                                   .GetSave<CharactersSave>("Characters");
            characterSave.onSelectedCharacterChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (characterSave != null)
                characterSave.onSelectedCharacterChanged -= Refresh;
        }

        private void Refresh()
        {
            // 1) current character & level
            var charData  = charactersDatabase.GetCharacterData(characterSave.SelectedCharacterId);
            int charLevel = CharacterLevelSystem.GetLevel(charData);

            // 2) clear out any old icons
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            // 3) loop through the DB
            for (int i = 0; i < abilitiesDatabase.AbilitiesCount; i++)
            {
                var ad = abilitiesDatabase.GetAbility(i);
                if (ad == null || !ad.IsEvolution)
                    continue;

                // 4) filter by character restriction
                if (ad.IsCharacterSpecific && ad.AllowedCharacterName != charData.Name)
                    continue;

                // 5) check the *character* level unlock, not your ability‐level requirement
                int reqLvl   = ad.MinCharacterLevel;
                bool unlocked = charLevel >= reqLvl;

                Debug.Log($"[EvoUI] '{ad.Title}' → charLvl={charLevel}, minCharLvl={reqLvl}, unlocked={unlocked}");

                // 6) spawn & configure
                var go = Instantiate(evoItemPrefab, container, false);
                go.GetComponent<EvoAbilityItemBehavior>().Setup(ad.Icon, unlocked);
            }
        }

    }
}
