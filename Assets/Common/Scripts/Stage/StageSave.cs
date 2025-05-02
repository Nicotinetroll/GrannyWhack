/************************************************************
 *  StageSave.cs – persistent run / progress data
 ************************************************************/
using System;
using System.Reflection;                 // ← NEW
using OctoberStudio.Save;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    public class StageSave : ISave
    {
        /*──────────── basic stage progression ───────────*/
        [SerializeField] int   maxReachedStageId;
        [SerializeField] int   selectedStageId;

        [SerializeField] bool  isPlaying;
        [SerializeField] float time;
        [SerializeField] bool  resetAbilities;          // true => wipe XP etc. on load

        /*──────────── rerolls ───────────*/
        [SerializeField] int   rerollCharges;           // ✅ stored within a run

        /*──────────── XP / kills / playtime ───────────*/
        [SerializeField] int   xpLevel;
        [SerializeField] float xp;
        [SerializeField] int   enemiesKilled;
        [SerializeField] float timeAlive;               // added field

        /*──────────── damage stats (overlay window) ───────────*/
        [SerializeField] private float totalDamage;
        [SerializeField] private float dps;

        /*──────────── runtime helpers ───────────*/
        public bool loadedBefore = false;

        public event UnityAction<int> onSelectedStageChanged;

        /*──────────── public props ───────────*/
        // stage selection / progress
        public int  SelectedStageId           => selectedStageId;
        public int  MaxReachedStageId         => maxReachedStageId;
        public bool IsFirstStageSelected      => selectedStageId == 0;
        public bool IsMaxReachedStageSelected => selectedStageId == maxReachedStageId;

        // run flags & data
        public bool  IsPlaying        { get => isPlaying;       set => isPlaying = value; }
        public float Time             { get => time;            set => time      = value; }
        public bool  ResetStageData   { get => resetAbilities;  set => resetAbilities = value; }

        // XP
        public int   XPLEVEL          { get => xpLevel;         set => xpLevel   = value; }
        public float XP               { get => xp;              set => xp        = value; }
        public int   EnemiesKilled    { get => enemiesKilled;   set => enemiesKilled = value; }

        // rerolls
        public int   RerollCharges    { get => rerollCharges;   set => rerollCharges = value; }

        // play‑time overlay
        public float TimeAlive        { get => timeAlive;       set => timeAlive = value; }
        public float TotalDamage      { get => totalDamage;     set => totalDamage = value; }
        public float DPS              { get => dps;             set => dps = value; }

        /*──────────── mutators ───────────*/
        public void SetSelectedStageId(int id)
        {
            selectedStageId = id;
            onSelectedStageChanged?.Invoke(id);
        }
        public void SetMaxReachedStageId(int id) => maxReachedStageId = id;

        /*──────────── ISave impl. ───────────*/
        public void Flush()
        {
            /* no custom binary flush – SaveManager serialises us */
        }

        /// <summary>Hard reset used by Dev‑popup: zeroes every field.</summary>
        public void ResetAll()
        {
            maxReachedStageId = 0;
            selectedStageId   = 0;
            isPlaying         = false;
            time              = 0f;
            resetAbilities    = false;
            rerollCharges     = 0;
            xpLevel           = 0;
            xp                = 0f;
            enemiesKilled     = 0;
            totalDamage       = 0f;
            dps               = 0f;
            timeAlive         = 0f;

            onSelectedStageChanged?.Invoke(0);
            Debug.Log("[StageSave] ResetAll ▶ stage progress wiped");
        }
    }
}
