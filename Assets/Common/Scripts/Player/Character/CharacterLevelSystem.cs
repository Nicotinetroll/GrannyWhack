using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio
{
    public static class CharacterLevelSystem
    {
        /* ── runtime refs ───────────────────────────────────────────── */
        static CharacterLevelSave      save;
        static CharacterLevelingConfig cfg;

        /* ── lazy bootstrap ─────────────────────────────────────────── */
        static void EnsureInit()
        {
            if (save != null) return;                       // already ready

            if (GameController.SaveManager == null)
            {
                Debug.LogWarning("[CharacterLevelSystem] SaveManager not ready. XP event skipped.");
                return;     // can’t do anything yet
            }

            save = GameController.SaveManager
                                  .GetSave<CharacterLevelSave>("CharacterLevels");

            cfg  = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            if (cfg == null)
                Debug.LogWarning("[CharacterLevelSystem] No CharacterLevelingConfig asset found.");
        }

        /* ── public API ─────────────────────────────────────────────── */
        public static int   GetLevel(CharacterData c) { EnsureInit(); return Get(c)?.lvl ?? 1; }
        public static float GetXp   (CharacterData c) { EnsureInit(); return Get(c)?.xp  ?? 0f; }

        public static void AddMatchResults(CharacterData c, int kills, float damage)
        {
            EnsureInit();
            if (save == null || c == null) return;          // still no infrastructure? bail.

            var entry = Get(c);
            if (entry == null) return;

            float inc = kills * cfg.XpPerKill +
                        damage * cfg.XpPerDamage;

            int prev = entry.lvl;
            entry.xp += inc;
            RecalcLevel(entry);

            Debug.Log($"[Character‑XP] {c.Name}: +{inc:F0} XP (total {entry.xp:F0})  "
                    + $"Level {prev} → {entry.lvl}");
        }

        /* ── helpers ───────────────────────────────────────────────── */
        static CharacterLevelEntry Get(CharacterData c)
        {
            var e = save?.Entries.Find(x => x.name == c.Name);
            if (e != null) return e;

            if (save == null) return null;

            e = new CharacterLevelEntry { name = c.Name };
            save.Entries.Add(e);
            return e;
        }

        static void RecalcLevel(CharacterLevelEntry e)
        {
            if (cfg == null) return;

            int   lvl       = 1;
            float xpPending = e.xp;

            while (lvl < cfg.MaxLevel &&
                   xpPending >= cfg.GetXpForLevel(lvl + 1))
            {
                xpPending -= cfg.GetXpForLevel(lvl + 1);
                lvl++;
            }

            e.lvl = lvl;
        }
    }
}
