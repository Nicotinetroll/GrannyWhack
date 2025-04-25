using System.Collections.Generic;
using OctoberStudio.Save;
using System;

namespace OctoberStudio
{
    public static class CharacterKillSystem
    {
        private static Dictionary<string, int> kills = new Dictionary<string, int>();
        private static ISaveManager saveManager;
        private static CharacterKillSave killSave;

        public static void Init(ISaveManager manager)
        {
            if (saveManager != null) return;

            saveManager = manager;
            killSave = saveManager.GetSave<CharacterKillSave>("CharacterKillSave");
            killSave.Init();

            kills = killSave.ToDictionary();
        }

        public static int GetKills(CharacterData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Name))
                return 0;

            return kills.TryGetValue(data.Name, out int count) ? count : 0;
        }

        public static void AddKill(CharacterData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Name))
                return;

            if (kills.ContainsKey(data.Name))
                kills[data.Name]++;
            else
                kills[data.Name] = 1;

            killSave.FromDictionary(kills);
        }

        public static void SaveNow()
        {
            if (killSave == null) return;
            killSave.FromDictionary(kills); // Just make sure it's updated
        }
    }

    [System.Serializable]
    public class CharacterKillSave : ISave
    {
        [Serializable]
        public class Entry
        {
            public string name;
            public int totalKills;
        }

        [UnityEngine.SerializeField]
        private List<Entry> entries = new();

        public void Init()
        {
            if (entries == null)
                entries = new List<Entry>();
        }

        public Dictionary<string, int> ToDictionary()
        {
            var result = new Dictionary<string, int>();
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.name))
                    result[e.name] = e.totalKills;
            }
            return result;
        }

        public void FromDictionary(Dictionary<string, int> dict)
        {
            entries.Clear();
            foreach (var kvp in dict)
            {
                entries.Add(new Entry
                {
                    name = kvp.Key,
                    totalKills = kvp.Value
                });
            }
        }

        public void Flush()
        {
            // Nothing needed, Unity will auto serialize `entries`
        }
    }
}
