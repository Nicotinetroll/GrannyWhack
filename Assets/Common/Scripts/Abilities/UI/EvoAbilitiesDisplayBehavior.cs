using System;
using System.Reflection;
using UnityEngine;
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
        [SerializeField] private CharactersSave      characterSave;   // optional, will auto‑fetch if left blank
        [SerializeField] private GameObject          evoItemPrefab;   // prefab with EvoAbilityItemBehavior

        [Header("Layout")]
        [SerializeField] private RectTransform       container;       // your “Evo Upgrades” HorizontalLayoutGroup

        private void Start()
        {
            if (characterSave == null)
                characterSave = GameController
                                   .SaveManager
                                   .GetSave<CharactersSave>("Characters");

            // whenever the selected character changes, rebuild the row
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
            // 1) get current character & level
            var data  = charactersDatabase.GetCharacterData(characterSave.SelectedCharacterId);
            int level  = CharacterLevelSystem.GetLevel(data);

            // 2) clear out any old icons
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            // 3) loop through all your AbilityType values
            foreach (AbilityType type in Enum.GetValues(typeof(AbilityType)))
            {
                var ad = abilitiesDatabase.GetAbility(type);
                if (ad == null || !ad.IsEvolution)
                    continue;

                // 4) grab the first element of EvolutionRequirements
                int reqLevel = 0;
                var evoReqs  = ad.EvolutionRequirements;
                if (evoReqs != null && evoReqs.Count > 0)
                {
                    object first = evoReqs[0];
                    var    t     = first.GetType();

                    // try a public field called "Level" or "RequiredLevel"
                    var fld = t.GetField("Level", BindingFlags.Public|BindingFlags.Instance|BindingFlags.IgnoreCase)
                           ?? t.GetField("RequiredLevel", BindingFlags.Public|BindingFlags.Instance|BindingFlags.IgnoreCase);
                    if (fld != null && fld.FieldType == typeof(int))
                    {
                        reqLevel = (int)fld.GetValue(first);
                    }
                    else
                    {
                        // or try a property of the same names
                        var prop = t.GetProperty("Level", BindingFlags.Public|BindingFlags.Instance|BindingFlags.IgnoreCase)
                                ?? t.GetProperty("RequiredLevel", BindingFlags.Public|BindingFlags.Instance|BindingFlags.IgnoreCase);
                        if (prop != null && prop.PropertyType == typeof(int))
                        {
                            reqLevel = (int)prop.GetValue(first);
                        }
                    }
                }

                // 5) decide if unlocked
                bool unlocked = level >= reqLevel;

                // 6) instantiate one generic prefab & drive its icon + lock state
                var go = Instantiate(evoItemPrefab, container);
                go.GetComponent<EvoAbilityItemBehavior>()
                  .Setup(ad.Icon, unlocked);
            }
        }
    }
}
