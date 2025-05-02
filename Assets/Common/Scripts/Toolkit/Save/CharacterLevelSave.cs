/************************************************************
 *  CharacterLevelSave.cs  –  per‑character XP / Level data
 ************************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio
{
    [Serializable]
    public class CharacterLevelEntry
    {
        public string name;
        public int    lvl = 1;
        public float  xp  = 0f;
    }

    [Serializable]
    public class CharacterLevelSave : ISave
    {
        [SerializeField] public List<CharacterLevelEntry> Entries = new();

        /*──────────────────────── ISave ────────────────────────*/
        public void Flush()  { /* nothing special needed */ }

        public void Clear() => Entries.Clear();

        /// <summary>
        /// Hard‑reset: wipes *all* saved level data.
        /// </summary>
        public void ResetAll()
        {
            Debug.Log("[CharacterLevelSave] FULL RESET – every character set to level 1, 0 XP.");
            Entries.Clear();                    // drop everything
            // After this, CharacterLevelSystem will lazily recreate fresh
            // entries on demand (all start at lvl 1, xp 0).
        }
    }
}