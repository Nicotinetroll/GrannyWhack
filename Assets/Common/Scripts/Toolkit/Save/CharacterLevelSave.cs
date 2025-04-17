using System;
using System.Collections.Generic;
using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio
{
    /// <summary>One row per hero.</summary>
    [Serializable]
    public class CharacterLevelEntry
    {
        public string name;
        public int    lvl = 1;
        public float  xp;
    }

    /// <summary>Save blob consumed by <see cref="CharacterLevelSystem"/>.</summary>
    [Serializable]
    public class CharacterLevelSave : ISave
    {
        [SerializeField] public List<CharacterLevelEntry> Entries = new();

        // ISave contract – nothing special to flush.
        public void Flush() { }
    }
}