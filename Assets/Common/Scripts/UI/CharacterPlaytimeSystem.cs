using System;
using System.Collections.Generic;
using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio
{
    /// <summary>
    /// Tracks total cumulative play time per character (in seconds).
    /// </summary>
    public static class CharacterPlaytimeSystem
    {
        [Serializable]
        class Entry { public string name; public float totalTime; }

        [Serializable]
        class PlaytimeSave : ISave
        {
            [SerializeField] List<Entry> entries = new();
            internal List<Entry> Entries => entries;
            public void Flush() { }  // list is serialized automatically
        }

        static PlaytimeSave save;

        /// <summary>
        /// Must be called once at game start (before usage).
        /// </summary>
        public static void Init(ISaveManager saveManager)
        {
            if (save != null) return;
            save = saveManager.GetSave<PlaytimeSave>("CharacterPlaytimes");
        }

        /// <summary>
        /// Adds the given seconds to the total for character c.
        /// </summary>
        public static void AddTime(CharacterData c, float seconds)
        {
            if (save == null || c == null) return;
            var e = save.Entries.Find(x => x.name == c.Name);
            if (e == null)
            {
                e = new Entry { name = c.Name };
                save.Entries.Add(e);
            }
            e.totalTime += seconds;
        }

        /// <summary>
        /// Returns cumulative total seconds for character c.
        /// </summary>
        public static float GetTime(CharacterData c)
        {
            if (save == null || c == null) return 0f;
            var e = save.Entries.Find(x => x.name == c.Name);
            return e?.totalTime ?? 0f;
        }
    }
}