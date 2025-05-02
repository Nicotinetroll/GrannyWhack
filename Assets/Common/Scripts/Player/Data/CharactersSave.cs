using OctoberStudio.Save;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    /// <summary>
    /// Ukladá kúpené postavy, aktuálne vybranú postavu
    /// + DEV‑prepísané Damage/HP.
    /// </summary>
    public class CharactersSave : ISave
    {
        /* ------- serialised ------- */
        [SerializeField] int[]  boughtCharacterIds;
        [SerializeField] int    selectedCharacterId;
        [SerializeField] float  characterDamage  = 0f;   // 0 = nepoužiť override
        [SerializeField] float  characterHealth  = 0f;   // 0 = nepoužiť override

        /* ------- runtime ------- */
        List<int> bought;
        
        /* ------- public API ------- */
        public int   SelectedCharacterId
        {
            get => selectedCharacterId;
            private set => selectedCharacterId = value;
        }

        public float CharacterDamage
        {
            get => characterDamage;
            set => characterDamage = Mathf.Max(0f, value);
        }
        public float CharacterHealth
        {
            get => characterHealth;
            set => characterHealth = Mathf.Max(0f, value);
        }

        /// <summary> Notifikuje ID aktuálne vybratej postavy – int arg. </summary>
        public UnityAction<int> onSelectedCharacterChanged;

        /* ------- Init / Flush ------- */
        public void Init()
        {
            if (boughtCharacterIds == null || boughtCharacterIds.Length == 0)
            {
                boughtCharacterIds = new[] { 0 };     // prvá postava zdarma
                selectedCharacterId = 0;
            }
            bought = new List<int>(boughtCharacterIds);
        }

        public void Flush()
        {
            if (bought == null) Init();
            boughtCharacterIds = bought.ToArray();
        }

        /* ------- helpers ------- */
        public bool HasCharacterBeenBought(int id)
        {
            if (bought == null) Init();
            return bought.Contains(id);
        }

        public void AddBoughtCharacter(int id)
        {
            if (bought == null) Init();
            if (!bought.Contains(id)) bought.Add(id);
        }

        public void SetSelectedCharacterId(int id)
        {
            if (bought == null) Init();
            SelectedCharacterId = id;
            onSelectedCharacterChanged?.Invoke(id);
        }
    }
}
