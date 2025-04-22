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
        [SerializeField]
        public List<CharacterLevelEntry> Entries = new();

        public void Flush() { /* nothing special */ }
        public void Clear() => Entries.Clear();
    }
}