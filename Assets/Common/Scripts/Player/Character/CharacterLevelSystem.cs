using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio
{
    public static class CharacterLevelSystem
    {
        static CharacterLevelSave      save;
        static CharacterLevelingConfig cfg;

        static void EnsureInit()
        {
            if (save != null && cfg != null) return;
            if (GameController.SaveManager == null) return;

            save = GameController.SaveManager.GetSave<CharacterLevelSave>("CharacterLevels");
            cfg  = Resources.Load<CharacterLevelingConfig>("CharacterLevelingConfig");
            if (cfg == null)
                Debug.LogWarning("[CharacterLevelSystem] Missing CharacterLevelingConfig.");
        }

        public static int   GetLevel(CharacterData c) { EnsureInit(); return GetEntry(c)?.lvl ?? 1; }
        public static float GetXp   (CharacterData c) { EnsureInit(); return GetEntry(c)?.xp  ?? 0f; }

        public static float GetDamageBonus(CharacterData c)
        {
            EnsureInit();
            int lvl = GetLevel(c);
            return (lvl - 1) * (cfg?.DamagePerLevel ?? 0f);
        }

        public static int MaxLevel
        {
            get { EnsureInit(); return cfg != null ? cfg.MaxLevel : 1; }
        }

        public static void SetLevel(CharacterData c, int level)
        {
            EnsureInit();
            if (save == null || c == null) return;
            var e = GetEntry(c);
            e.lvl = Mathf.Clamp(level, 1, MaxLevel);
        }

        /// <summary>
        /// Award XP for kills (damage ignored here). Recalculates level.
        /// </summary>
        public static void AddMatchResults(CharacterData c, int kills, float damage)
        {
            EnsureInit();
            if (save == null || cfg == null || c == null) return;

            var e   = GetEntry(c);
            float inc = kills * cfg.XpPerKill;
            e.xp += inc;
            RecalcLevel(e);
            Debug.Log($"[Character‑XP] {c.Name}: +{inc:F0} XP from kills → Level {e.lvl}");
        }

        /// <summary>
        /// Award XP each time the PLAYER levels up.
        /// </summary>
        public static void AddPlayerLevelResults(CharacterData c, int times)
        {
            EnsureInit();
            if (save == null || cfg == null || c == null) return;

            var e = GetEntry(c);
            float inc = times * cfg.XpPerPlayerLevel;
            e.xp += inc;
            RecalcLevel(e);
            Debug.Log($"[Character‑XP] {c.Name}: +{inc:F0} XP from player levels → Level {e.lvl}");
        }

        static CharacterLevelEntry GetEntry(CharacterData c)
        {
            EnsureInit();
            var e = save.Entries.Find(x => x.name == c.Name);
            if (e != null) return e;
            e = new CharacterLevelEntry { name = c.Name };
            save.Entries.Add(e);
            return e;
        }

        static void RecalcLevel(CharacterLevelEntry e)
        {
            if (cfg == null) return;
            int   lvl  = 1;
            float xp   = e.xp;
            while (lvl < cfg.MaxLevel &&
                   xp >= cfg.GetCumulativeXpForLevel(lvl + 1))
            {
                lvl++;
            }
            e.lvl = lvl;
        }
    }
}
