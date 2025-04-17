// CharacterLevelSystem.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio
{
    /// <summary>Static helper – zero MonoBehaviours, zero Update spam.</summary>
    public static class CharacterLevelSystem
    {
        #region --- internal save -----------------------------------------------
        [Serializable] class Entry { public string name; public int lvl = 1; public float xp; }

        [Serializable] class CharacterLevelSave : ISave
        {
            [SerializeField] List<Entry> entries = new();
            internal List<Entry> Entries => entries;
            public void Flush() { }                          // nothing to do – list is serialised as‑is
        }
        #endregion

        static CharacterLevelSave   save;
        static CharacterLevelingConfig cfg;

        public static void Init(ISaveManager sm)
        {
            if (save != null) return;                       // already initialised
            save = sm.GetSave<CharacterLevelSave>("CharacterLevels");
            cfg  = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            if (cfg == null) Debug.LogWarning(
                "[CharacterLevelSystem] No CharacterLevelingConfig found – using hard‑wired defaults.");
        }

        // ---------------- public API ---------------------------------------------------

        public static int   GetLevel(CharacterData c) => Get(c).lvl;
        public static float GetXp   (CharacterData c) => Get(c).xp;

        // ── inside CharacterLevelSystem.cs ─────────────────────────────────
        public static void AddMatchResults(CharacterData c, int kills, float damage)
        {
            var e   = Get(c);

            // XP gained this run
            var inc = kills  * cfg.XpPerKill +
                      damage * cfg.XpPerDamage;

            int prevLevel = e.lvl;           // keep for logging

            e.xp += inc;
            RecalcLevel(e);

            // --- Console output ------------------------------------------------------
            Debug.Log(
                $"[Character‑XP] {c.Name}:  +{inc:F0} XP  (total {e.xp:F0})   "
                + $"Level {prevLevel} → {e.lvl}");
        }


        #region --- helpers -------------------------------------------------------------
        static Entry Get(CharacterData c)
        {
            var e = save.Entries.Find(x => x.name == c.Name);
            if (e != null) return e;
            e = new Entry { name = c.Name };
            save.Entries.Add(e);
            return e;
        }

        static void RecalcLevel(Entry e)
        {
            int lvl = 1;
            float xpPending = e.xp;
            while (lvl < cfg.MaxLevel && xpPending >= cfg.GetXpForLevel(lvl + 1))
            {
                xpPending -= cfg.GetXpForLevel(lvl + 1);
                lvl++;
            }
            e.lvl = lvl;
        }
        #endregion
    }
}
