using OctoberStudio.Save;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    public class StageSave : ISave
    {
        [SerializeField] int maxReachedStageId;
        [SerializeField] int selectedStageId;

        [SerializeField] bool isPlaying;
        [SerializeField] float time;
        [SerializeField] bool resetAbilities;

        [SerializeField] int rerollCharges; // ✅ Stored reroll charges

        [SerializeField] int xpLevel;
        [SerializeField] float xp;
        [SerializeField] int enemiesKilled;
        
        [SerializeField] private float timeAlive; // Add this to the top with the other fields
        


        public bool loadedBefore = false;

        public event UnityAction<int> onSelectedStageChanged;

        // STAGES
        public int SelectedStageId => selectedStageId;
        public int MaxReachedStageId => maxReachedStageId;
        public bool IsFirstStageSelected => selectedStageId == 0;
        public bool IsMaxReachedStageSelected => selectedStageId == maxReachedStageId;
        public bool IsPlaying { get => isPlaying; set => isPlaying = value; }

        public float Time { get => time; set => time = value; }
        public bool ResetStageData { get => resetAbilities; set => resetAbilities = value; }
        public int XPLEVEL { get => xpLevel; set => xpLevel = value; }
        public float XP { get => xp; set => xp = value; }
        public int EnemiesKilled { get => enemiesKilled; set => enemiesKilled = value; }
        
        /// 🔁 Reroll Charges: Stored during a game run (per session).
        public int RerollCharges
        {
            get => rerollCharges;
            set => rerollCharges = value;
        }
        
        public float TimeAlive
        {
            get => timeAlive;
            set => timeAlive = value;
        }
        
        // DAMAGE
        [SerializeField] private float totalDamage;
        [SerializeField] private float dps;

        public float TotalDamage { get => totalDamage; set => totalDamage = value; }
        public float DPS { get => dps; set => dps = value; }


        public void SetSelectedStageId(int selectedStageId)
        {
            this.selectedStageId = selectedStageId;
            onSelectedStageChanged?.Invoke(selectedStageId);
        }

        public void SetMaxReachedStageId(int maxReachedStageId)
        {
            this.maxReachedStageId = maxReachedStageId;
        }

        public void Flush()
        {
            // Optional: implement custom flushing if needed
        }
    }
}
