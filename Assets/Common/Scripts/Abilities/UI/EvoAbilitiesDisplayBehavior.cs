using System.Collections.Generic;
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
            // 1) Fetch current character & level
            var charData = charactersDatabase.GetCharacterData(characterSave.SelectedCharacterId);
            int charLevel = CharacterLevelSystem.GetLevel(charData);

            // 2) Clear out old icons
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            Debug.Log($"[EvoUI] Refresh for '{charData.Name}' (lvl {charLevel})");

            // 3) Gather only this character’s EVO abilities
            var evoList = new List<AbilityData>();
            for (int i = 0; i < abilitiesDatabase.AbilitiesCount; i++)
            {
                var ad = abilitiesDatabase.GetAbility(i);
                if (ad == null || !ad.IsEvolution)
                    continue;

                // ✂️ SKIP any global EVO (must be character‑specific)
                if (!ad.IsCharacterSpecific)
                    continue;

                // ✂️ SKIP if it isn’t assigned to this character
                if (ad.AllowedCharacterName != charData.Name)
                    continue;

                evoList.Add(ad);
            }

            // 4) Sort low→high unlock‑level
            evoList.Sort((a, b) => a.MinCharacterLevel.CompareTo(b.MinCharacterLevel));

            // 5) Spawn them in order
            foreach (var ad in evoList)
            {
                bool unlocked = charLevel >= ad.MinCharacterLevel;
                Debug.Log($"[EvoUI] SPAWNING '{ad.Title}'  charLvl={charLevel}, minCharLvl={ad.MinCharacterLevel}, unlocked={unlocked}");

                var go = Instantiate(evoItemPrefab, container, false);
                go.GetComponent<EvoAbilityItemBehavior>().Setup(ad.Icon, unlocked);
            }
        }



    }
}
