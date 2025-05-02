/************************************************************
 *  CharactersSave.cs  –  persistent data for owned heroes
 *  2025‑05‑04  (hot‑fix: null‑safe boughtList + ResetAll)
 ************************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio.Save;

namespace OctoberStudio
{
    public class CharactersSave : ISave
    {
    /* ───── stored fields (serialised) ───── */
        [SerializeField] int[]  boughtCharacterIds;         // ids the player owns
        [SerializeField] int    selectedCharacterId;        // id currently selected

        [SerializeField] float  characterDamage = 0f;       // DEV overrides
        [SerializeField] float  characterHealth = 0f;

    /* ───── runtime helpers ───── */
        List<int> boughtList;                               // fast lookup list

    /* ───── events / public access ───── */
        public int   SelectedCharacterId            => selectedCharacterId;
        public float CharacterDamage { get => characterDamage; set => characterDamage = value; }
        public float CharacterHealth { get => characterHealth; set => characterHealth = value; }

        /// <summary>Fired after every call to <c>SetSelectedCharacterId()</c>.</summary>
        public UnityAction<int> onSelectedCharacterChanged;

    /* ───────────────────────────────────────────────────────────────
     *  INTERNAL GUARD
     *  Guarantees:
     *    • boughtCharacterIds is never null and always contains 0
     *    • boughtList is always a valid List<int>
     *  Call this at the start of every public method that accesses
     *  either field.  (It is cheap and 100 % safe.)
     * ─────────────────────────────────────────────────────────────── */
        void EnsureList()
        {
            if (boughtCharacterIds == null || boughtCharacterIds.Length == 0)
                boughtCharacterIds = new[] { 0 };           // first hero unlocked

            boughtList ??= new List<int>(boughtCharacterIds);
        }

    /* ───── ISave boiler‑plate ───── */
        public void Init()
        {
            EnsureList();
            selectedCharacterId = Mathf.Clamp(selectedCharacterId, 0, int.MaxValue);
        }

        public void Flush()
        {
            EnsureList();
            boughtCharacterIds = boughtList.ToArray();
        }

    /* ───── queries / mutations ───── */
        public bool HasCharacterBeenBought(int id)
        {
            EnsureList();
            return boughtList.Contains(id);
        }

        public void AddBoughtCharacter(int id)
        {
            EnsureList();
            if (!boughtList.Contains(id)) boughtList.Add(id);
        }

        public void SetSelectedCharacterId(int id)
        {
            EnsureList();
            selectedCharacterId = id;
            onSelectedCharacterChanged?.Invoke(id);
        }

    /* ───── hard‑reset (called from DevPopup) ───── */
    public void ResetAll()
    {
        /* character ownership */
        boughtCharacterIds  = new[] { 0 };
        boughtList          = new List<int> { 0 };
        selectedCharacterId = 0;

        /* dev overrides */
        characterDamage = 0f;
        characterHealth = 0f;

        /* fire event so every Character‑UI redraws immediately */
        onSelectedCharacterChanged?.Invoke(0);

        Debug.Log("[CharactersSave] ResetAll ▶ wiped characters, damage & health overrides");
    }
    }
}
