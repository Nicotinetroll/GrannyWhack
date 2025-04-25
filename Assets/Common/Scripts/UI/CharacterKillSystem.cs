using System.Collections.Generic;
using OctoberStudio.Save;

namespace OctoberStudio
{
    public static class CharacterKillSystem
    {
        private static Dictionary<string, int> kills = new Dictionary<string, int>();
        private static ISaveManager saveManager;
        private static CharacterKillSave killSave;

        public static void Init(ISaveManager manager)
        {
            if (saveManager != null)
                return;

            saveManager = manager;
            killSave = saveManager.GetSave<CharacterKillSave>("CharacterKillSave");
            killSave.Init();

            kills = killSave.LoadKills();
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

            killSave.SaveKills(kills);
            saveManager.Save(true); // Corrected to match actual signature
        }
    }

    [System.Serializable]
    public class CharacterKillSave : ISave
    {
        public Dictionary<string, int> savedKills = new Dictionary<string, int>();

        public void Init()
        {
            if (savedKills == null)
                savedKills = new Dictionary<string, int>();
        }

        public Dictionary<string, int> LoadKills()
        {
            return savedKills ?? new Dictionary<string, int>();
        }

        public void SaveKills(Dictionary<string, int> killsToSave)
        {
            savedKills = new Dictionary<string, int>(killsToSave);
        }

        public void Flush()
        {
            savedKills.Clear();
        }
    }
}
