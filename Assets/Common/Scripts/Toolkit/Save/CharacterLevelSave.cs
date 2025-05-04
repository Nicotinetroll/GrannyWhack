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

        public void Flush()
        {
            // Only write out our current entries.
            // Do NOT reset or clear Entries here!
        }

        public void Clear() => Entries.Clear();

        public void ResetAll()
        {
            // FULL RESET only when you want to wipe everything.
            Debug.Log("[CharacterLevelSave] FULL RESET – every character set to level 1, 0 XP.");
            Entries.Clear();
        }
    }
}