using System;
using OctoberStudio.Save;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    public class CharactersSave : ISave
    {
        /* ── stored data ─────────────────────────────── */
        [SerializeField] int[] boughtCharacterIds;
        [SerializeField] int   selectedCharacterId;

        [SerializeField] float characterDamage = 0f;
        [SerializeField] float characterHealth = 0f;

        /* ── public API ──────────────────────────────── */
        public int   SelectedCharacterId            => selectedCharacterId;
        public float CharacterDamage { get => characterDamage; set => characterDamage = value; }
        public float CharacterHealth { get => characterHealth; set => characterHealth = value; }

        /// <summary>Fires with the **new id** each time the selection changes.</summary>
        public UnityAction<int> onSelectedCharacterChanged;

        List<int> boughtList;

        /* ── init / flush ───────────────────────────── */
        public void Init()
        {
            /* -------------------------------------------------
             * make sure boughtCharacterIds is never null
             * ------------------------------------------------- */
            if (boughtCharacterIds == null || boughtCharacterIds.Length == 0)
            {
                boughtCharacterIds = new[] { 0 };  // first character is always owned
                selectedCharacterId = 0;
            }

            /* existing logic – now guaranteed to get a non‑null array */
            boughtList = new List<int>(boughtCharacterIds);
        }

        public void Flush()
        {
            if (boughtList == null) Init();
            boughtCharacterIds = boughtList.ToArray();
        }

        /* ── queries & mutations ────────────────────── */
        public bool HasCharacterBeenBought(int id)
        {
            boughtList ??= new List<int>(boughtCharacterIds);
            return boughtList.Contains(id);
        }

        public void AddBoughtCharacter(int id)
        {
            boughtList ??= new List<int>(boughtCharacterIds);
            if (!boughtList.Contains(id)) boughtList.Add(id);
        }

        public void SetSelectedCharacterId(int id)
        {
            boughtList ??= new List<int>(boughtCharacterIds);
            selectedCharacterId = id;
            onSelectedCharacterChanged?.Invoke(id);
        }

        /* ── hard‑reset helper for DevPopup ─────────── */
        public void ResetAll()
        {
            /* ───── core data ───── */
            boughtCharacterIds  = new[] { 0 };      // player owns character 0 again
            boughtList          = new List<int> { 0 };
            selectedCharacterId = 0;

            /* ───── DEV overrides ───── */
            characterDamage     = 0f;
            characterHealth     = 0f;

            /* ───── notify & log ───── */
            onSelectedCharacterChanged?.Invoke(0);
            Debug.Log("[CharactersSave] ResetAll ▶ everything wiped");
        }
    }
}
