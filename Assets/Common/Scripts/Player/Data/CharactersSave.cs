using OctoberStudio.Save;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    public class CharactersSave : ISave
    {
        /* ‑‑‑‑ kúpené & výber ‑‑‑‑ */
        [SerializeField] int[] boughtCharacterIds;
        [SerializeField] int   selectedCharacterId;

        /* ‑‑‑‑ DEV nastaviteľné štatistiky ‑‑‑‑ */
        [SerializeField] float characterDamage = 1f;
        [SerializeField] float characterHealth = 50f;

        /* public API */
        public int   SelectedCharacterId  => selectedCharacterId;
        public float CharacterDamage      { get => characterDamage; set => characterDamage = value; }
        public float CharacterHealth      { get => characterHealth; set => characterHealth = value; }

        public UnityAction onSelectedCharacterChanged;

        List<int> boughtList;

        /* ───────────────────── init ───────────────────── */
        public void Init()
        {
            if (boughtCharacterIds == null || boughtCharacterIds.Length == 0)
            {
                boughtCharacterIds = new[] { 0 };   // prvá postava zdarma
                selectedCharacterId = 0;
            }
            boughtList = new List<int>(boughtCharacterIds);
        }

        /* ───────────────────── store / query ───────────────────── */
        public bool HasCharacterBeenBought(int id)
        {
            if (boughtList == null) Init();
            return boughtList.Contains(id);
        }

        public void AddBoughtCharacter(int id)
        {
            if (boughtList == null) Init();
            if (!boughtList.Contains(id)) boughtList.Add(id);
        }

        public void SetSelectedCharacterId(int id)
        {
            if (boughtList == null) Init();
            selectedCharacterId = id;
            onSelectedCharacterChanged?.Invoke();
        }

        /* ───────────────────── save flush ───────────────────── */
        public void Flush()
        {
            if (boughtList == null) Init();
            boughtCharacterIds = boughtList.ToArray();
        }
    }
}
