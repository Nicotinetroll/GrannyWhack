using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio;          // gives CharacterData + CharacterLevelSystem

namespace OctoberStudio.Abilities
{
    public abstract class AbilityData : ScriptableObject
    {
        /* ───────── Character restriction ───────── */
        [Header("Character Restriction")]
        [SerializeField] bool  isCharacterSpecific = false;
        public  bool  IsCharacterSpecific => isCharacterSpecific;

        [SerializeField] string allowedCharacterName;
        public  string AllowedCharacterName => allowedCharacterName;

        [SerializeField, Min(1)] int minCharacterLevel = 1;
        public  int MinCharacterLevel => minCharacterLevel;
        /* ───────────────────────────────────────── */

        [Tooltip("The unique identifier of an ability")]
        [SerializeField] protected AbilityType type;
        public AbilityType AbilityType => type;

        [Tooltip("Should be short, no more than two words")]
        [SerializeField] string title;
        public string Title => title;

        [Tooltip("Keep it brief but informative")]
        [SerializeField] string description;
        public string Description => description;

        [Tooltip("Image that will appear on the UI")]
        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [Tooltip("Prefab with the implementation of the ability")]
        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;

        [SerializeField] protected bool isActiveAbility;
        public bool IsActiveAbility => isActiveAbility;

        [Tooltip("Whether this ability is linked to the weapon. It will be shown to the player only if the character has the linked weapon equipped")]
        [SerializeField] protected bool isWeaponAbility;
        public bool IsWeaponAbility => isWeaponAbility;

        [Tooltip("Shown only when there are no other abilities available. Cannot be upgraded, always applies its first level")]
        [SerializeField] protected bool isEndgameAbility;
        public bool IsEndgameAbility => isEndgameAbility;

        [Tooltip("Whether this ability is the evolution of other abilities. It will be shown only if the evolution requirements are met")]
        [SerializeField] bool isEvolution;
        public bool IsEvolution => isEvolution;

        [Tooltip("The requirements for this ability to be shown. Ignored if 'isEvolution' is false")]
        [SerializeField] List<EvolutionRequirement> evolutionRequirements;
        public List<EvolutionRequirement> EvolutionRequirements => evolutionRequirements;

        public abstract AbilityLevel[] Levels { get; }
        public int LevelsCount => Levels.Length;

        public event UnityAction<int> onAbilityUpgraded;

        public void Upgrade(int level) => onAbilityUpgraded?.Invoke(level);

        public AbilityLevel GetLevel(int index) => Levels[index];

        /* ───── helper for gating ───── */
        public bool IsUnlockedFor(CharacterData character)
        {
            if (!isCharacterSpecific) return true;
            if (character == null)     return false;

            return character.Name == allowedCharacterName &&
                   CharacterLevelSystem.GetLevel(character) >= minCharacterLevel;
        }
    }

    /* ---------- serialisable helpers ---------- */

    [System.Serializable]
    public abstract class AbilityLevel { }

    [System.Serializable]
    public class EvolutionRequirement
    {
        [Tooltip("This ability should be active to trigger the evolution")]
        [SerializeField] AbilityType abilityType;
        public AbilityType AbilityType => abilityType;

        [Tooltip("The level of the ability needed to trigger the evolution")]
        [SerializeField, Min(0)] int requiredAbilityLevel;
        public int RequiredAbilityLevel => requiredAbilityLevel;

        [Tooltip("Whether this ability should be disabled after the evolution")]
        [SerializeField] bool shouldRemoveAfterEvolution;
        public bool ShouldRemoveAfterEvolution => shouldRemoveAfterEvolution;
    }
}
