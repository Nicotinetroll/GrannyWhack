using UnityEngine;
using UnityEngine.UI;             // for LayoutRebuilder
using OctoberStudio.Abilities;
using OctoberStudio.Save;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class EvoAbilitiesDisplayBehavior : MonoBehaviour
    {
        [Header("Data & Prefabs")]
        [SerializeField] private CharactersDatabase charactersDatabase;
        [SerializeField] private AbilitiesDatabase  abilitiesDatabase;
        [SerializeField] private CharactersSave      characterSave;   // optional
        [SerializeField] private GameObject          evoItemPrefab;   // generic prefab

        [Header("Layout (must have a HorizontalLayoutGroup)")]
        [SerializeField] private RectTransform       container;

        private void Start()
        {
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
            // 1) Grab current char data & level
            var data           = charactersDatabase.GetCharacterData(characterSave.SelectedCharacterId);
            int characterLevel = CharacterLevelSystem.GetLevel(data);

            // 2) Clear old items
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            // 3) Spawn new Evo items
            for (int i = 0; i < abilitiesDatabase.AbilitiesCount; i++)
            {
                var ad = abilitiesDatabase.GetAbility(i);
                if (ad == null || !ad.IsEvolution)
                    continue;

                bool unlocked = characterLevel >= ad.MinCharacterLevel;

                // ←—— This ensures the item's RectTransform is reset into the container
                var go = Instantiate(evoItemPrefab, container, false);

                go.GetComponent<EvoAbilityItemBehavior>()
                  .Setup(ad.Icon, unlocked);
            }

            // 4) Now force the layout group to recalculate positions/sizes
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }
    }
}
