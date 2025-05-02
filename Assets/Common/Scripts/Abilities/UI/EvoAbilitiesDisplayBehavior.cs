// Assets/Common/Scripts/UI/EvoAbilitiesDisplayBehavior.cs
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
    /*────────── Inspector ──────────*/
        [Header("Data & Prefabs")]
        [SerializeField] CharactersDatabase charactersDatabase;
        [SerializeField] AbilitiesDatabase  abilitiesDatabase;
        [SerializeField] CharactersSave     characterSave;     // optional, doplní sa autom.
        [SerializeField] GameObject         evoItemPrefab;

        [Header("Layout")]
        [SerializeField] RectTransform      container;         // Grid/Horizontal Layout

    /*────────── Lifecycle ──────────*/
        void Start()
        {
            if (characterSave == null)
                characterSave = GameController
                                   .SaveManager
                                   .GetSave<CharactersSave>("Characters");

            characterSave.onSelectedCharacterChanged += Refresh;
            Refresh();          // initial draw
        }

        void OnDestroy()
        {
            if (characterSave != null)
                characterSave.onSelectedCharacterChanged -= Refresh;
        }

    /*────────── Public API ──────────*/
        /// <summary>
        /// Volaj, keď sa zmení level, damage alebo po výbere inej postavy.
        /// Wrapper bez parametra, aby sa dal pohodlne volať z Dev popupu.
        /// </summary>
        [ContextMenu("Refresh Display")]
        public void RefreshDisplay() => Refresh(0);

    /*────────── Core ──────────*/
        void Refresh(int _ = 0)
        {
            if (charactersDatabase == null || abilitiesDatabase == null || container == null)
            {
                Debug.LogWarning("[EvoUI] Missing references – refresh aborted.");
                return;
            }

            /* 1) Aktuálna postava + level */
            var charData = charactersDatabase.GetCharacterData(characterSave.SelectedCharacterId);
            int charLvl  = CharacterLevelSystem.GetLevel(charData);

            Debug.Log($"[EvoUI] Refresh for '{charData.Name}' (lvl {charLvl})");

            /* 2) Zmaž staré ikony */
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            /* 3) Nazbieraj EVO ability tejto postavy */
            var evo = new List<AbilityData>();
            for (int i = 0; i < abilitiesDatabase.AbilitiesCount; i++)
            {
                var ad = abilitiesDatabase.GetAbility(i);
                if (ad == null || !ad.IsEvolution || !ad.IsCharacterSpecific) continue;

                if (!ad.AllowedCharacterName.Equals(charData.Name,
                                                     StringComparison.OrdinalIgnoreCase)) continue;

                evo.Add(ad);
            }

            /* 4) Ak žiadne EVO – končíme */
            if (evo.Count == 0)
            {
                Debug.Log($"[EvoUI] No EVO abilities defined for '{charData.Name}'");
                return;
            }

            /* 5) Sort low→high a spawn */
            evo.Sort((a, b) => a.MinCharacterLevel.CompareTo(b.MinCharacterLevel));

            foreach (var ad in evo)
            {
                bool unlocked = charLvl >= ad.MinCharacterLevel;
                Debug.Log($"[EvoUI] SPAWNING '{ad.Title}'  " +
                          $"charLvl={charLvl}, minCharLvl={ad.MinCharacterLevel}, unlocked={unlocked}");

                var go = Instantiate(evoItemPrefab, container, false);
                go.GetComponent<EvoAbilityItemBehavior>().Setup(ad.Icon, unlocked);
            }
        }
    }
}
