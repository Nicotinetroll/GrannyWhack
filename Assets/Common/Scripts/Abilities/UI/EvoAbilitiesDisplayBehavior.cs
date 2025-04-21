// Assets/Common/Scripts/UI/EvoAbilitiesDisplayBehavior.cs
using System;
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
        [SerializeField] private CharactersDatabase charactersDatabase;
        [SerializeField] private AbilitiesDatabase  abilitiesDatabase;
        [SerializeField] private CharactersSave      characterSave;
        [SerializeField] private GameObject          evoItemPrefab;

        [Header("Layout")]
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
            // 1) Fetch current character & level
            var charData  = charactersDatabase.GetCharacterData(characterSave.SelectedCharacterId);
            int charLevel = CharacterLevelSystem.GetLevel(charData);

            Debug.Log($"[EvoUI] Refresh for '{charData.Name}' (lvl {charLevel})");

            // 2) Clear out old icons
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            // 3) Gather only this character’s EVO abilities
            var evoList = new List<AbilityData>();
            for (int i = 0; i < abilitiesDatabase.AbilitiesCount; i++)
            {
                var ad = abilitiesDatabase.GetAbility(i);
                if (ad == null || !ad.IsEvolution || !ad.IsCharacterSpecific)
                    continue;

                // now do a case‑insensitive match
                if (!string.Equals(ad.AllowedCharacterName,
                                   charData.Name,
                                   StringComparison.OrdinalIgnoreCase))
                    continue;

                evoList.Add(ad);
            }

            // 4) Sort low→high unlock‑level
            evoList.Sort((a, b) => a.MinCharacterLevel.CompareTo(b.MinCharacterLevel));

            if (evoList.Count == 0)
            {
                Debug.Log($"[EvoUI] No EVO abilities defined for '{charData.Name}'");
                // you may want to show a special "none available" label here
                return;
            }

            // 5) Spawn them in order
            foreach (var ad in evoList)
            {
                bool unlocked = charLevel >= ad.MinCharacterLevel;
                Debug.Log($"[EvoUI] SPAWNING '{ad.Title}'  " +
                          $"charLvl={charLevel}, minCharLvl={ad.MinCharacterLevel}, unlocked={unlocked}");

                var go = Instantiate(evoItemPrefab, container, false);
                go.GetComponent<EvoAbilityItemBehavior>().Setup(ad.Icon, unlocked);
            }
        }
    }
}
