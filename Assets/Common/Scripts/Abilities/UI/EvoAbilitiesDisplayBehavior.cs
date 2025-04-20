using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private CharactersSave      characterSave;   // optional, auto‑fetched if null
        [SerializeField] private GameObject          evoItemPrefab;

        [Header("Layout (must have a HorizontalLayoutGroup)")]
        [SerializeField] private RectTransform       container;

        private const string TargetCharacterName = "NIJNA";

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
            // 1) clear any existing icons
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            // 2) spawn only Evo abilities whose AllowedCharacterName == "NIJNA"
            for (int i = 0; i < abilitiesDatabase.AbilitiesCount; i++)
            {
                var ad = abilitiesDatabase.GetAbility(i);
                if (ad == null) 
                    continue;

                // must be an evolution ability
                if (!ad.IsEvolution) 
                    continue;

                // must be marked character‑specific to NIJNA
                if (!ad.IsCharacterSpecific || ad.AllowedCharacterName != TargetCharacterName)
                    continue;

                // instantiate under the layout group
                var go = Instantiate(evoItemPrefab, container, false);

                // configure the icon (always “unlocked” here)
                go.GetComponent<EvoAbilityItemBehavior>()
                  .Setup(ad.Icon, true);

                // reset RectTransform so layout group places it properly
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
                rt.localScale       = Vector3.one;
            }

            // 3) force the HorizontalLayoutGroup to rebuild positions
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }
    }
}
