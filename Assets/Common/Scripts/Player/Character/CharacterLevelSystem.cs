using UnityEngine;
using OctoberStudio.Save;
using System.Linq;

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
            if (save != null && cfg != null) return;

            if (GameController.SaveManager == null)
            {
                Debug.LogWarning("[CharacterLevelSystem] SaveManager not ready. Aborting init.");
                return;
            }

            save = GameController.SaveManager
                                  .GetSave<CharacterLevelSave>("CharacterLevels");

            cfg = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            if (cfg == null)
                Debug.LogWarning("[CharacterLevelSystem] No CharacterLevelingConfig asset found in Resources!");
        }

        /* ── public API ─────────────────────────────────────────────── */
        /// <summary>Current level (≥1).</summary>
        public static int GetLevel(CharacterData c)
        {
            EnsureInit();
            return GetEntry(c)?.lvl ?? 1;
        }

        /// <summary>Total accumulated XP.</summary>
        public static float GetXp(CharacterData c)
        {
            EnsureInit();
            return GetEntry(c)?.xp  ?? 0f;
        }

        /// <summary>Flat damage bonus = (level – 1) × damagePerLevel</summary>
        public static float GetDamageBonus(CharacterData c)
        {
            EnsureInit();
            int lvl = GetLevel(c);
            return (lvl - 1) * (cfg?.DamagePerLevel ?? 0f);
        }

        /// <summary>Maximum level from your config.</summary>
        public static int MaxLevel
        {
            get
            {
                EnsureInit();
                return cfg != null ? cfg.MaxLevel : 1;
            }
        }

        /// <summary>Force‑set a character’s level (for debug / tools).</summary>
        public static void SetLevel(CharacterData c, int level)
        {
            EnsureInit();
            if (save == null || c == null) return;

            var entry = GetEntry(c);
            if (entry == null) return;

            entry.lvl = Mathf.Clamp(level, 1, MaxLevel);
        }

        /// <summary>
        /// Grant XP **only** from kills; damage ignored.  Recalculates level and logs percent‑to‑next.
        /// </summary>
        public static void AddMatchResults(CharacterData c, int kills, float damage)
        {
            EnsureInit();
            if (save == null || c == null || cfg == null) return;

            var entry = GetEntry(c);
            if (entry == null) return;

            float inc = kills * cfg.XpPerKill;
            int prevLevel = entry.lvl;
            entry.xp += inc;

            // calculate percent towards next before we bump level
            float xpPending = entry.xp;
            int lvlCounter = 1;
            while (lvlCounter < cfg.MaxLevel &&
                   xpPending >= cfg.GetXpForLevel(lvlCounter + 1))
            {
                xpPending -= cfg.GetXpForLevel(lvlCounter + 1);
                lvlCounter++;
            }
            float xpForNext = (lvlCounter < cfg.MaxLevel)
                ? cfg.GetXpForLevel(lvlCounter + 1)
                : cfg.GetXpForLevel(lvlCounter);
            float percent = xpForNext > 0f
                ? (xpPending / xpForNext) * 100f
                : 100f;

            // now actually recalc level
            RecalcLevel(entry);

            Debug.Log(
                $"[Character‑XP] {c.Name}: +{inc:F0} XP from {kills} kills  " +
                $"(total {entry.xp:F0})  Level {prevLevel} → {entry.lvl}  " +
                $"{percent:F1}% to next");
        }

        /* ── private helpers ───────────────────────────────────────── */
        static CharacterLevelEntry GetEntry(CharacterData c)
        {
            EnsureInit();
            var e = save.Entries.Find(x => x.name == c.Name);
            if (e != null) return e;

            // create new entry
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
