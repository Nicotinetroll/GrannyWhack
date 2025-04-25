using OctoberStudio;               // for CharacterPlaytimeSystem
using OctoberStudio.Abilities;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using OctoberStudio.Timeline.Bossfight;
using OctoberStudio.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace OctoberStudio
{
    public class StageController : MonoBehaviour
    {
        private static StageController instance;
        private CharacterData     cachedCharacter;

        [SerializeField] StagesDatabase      database;
        [SerializeField] PlayableDirector    director;
        [SerializeField] EnemiesSpawner      spawner;
        [SerializeField] StageFieldManager   fieldManager;
        [SerializeField] ExperienceManager   experienceManager;
        [SerializeField] DropManager         dropManager;
        [SerializeField] AbilityManager      abilityManager;
        [SerializeField] PoolsManager        poolsManager;
        [SerializeField] WorldSpaceTextManager worldSpaceTextManager;
        [SerializeField] CameraManager       cameraManager;

        [Header("UI")]
        [SerializeField] GameScreenBehavior  gameScreen;
        [SerializeField] StageFailedScreen   stageFailedScreen;
        [SerializeField] StageCompleteScreen stageCompletedScreen;
        [SerializeField] PlayerStatsManager  playerStats;

        [Header("Testing")]
        [SerializeField] PresetData testingPreset;

        private StageSave stageSave;
        public static StageData Stage { get; private set; }

        // ─── Static accessors for all subsystems ────────────────────────
        public static EnemiesSpawner        EnemiesSpawner      => instance.spawner;
        public static ExperienceManager     ExperienceManager   => instance.experienceManager;
        public static AbilityManager        AbilityManager      => instance.abilityManager;
        public static StageFieldManager     FieldManager        => instance.fieldManager;
        public static PlayableDirector      Director            => instance.director;
        public static PoolsManager          PoolsManager        => instance.poolsManager;
        public static WorldSpaceTextManager WorldSpaceTextManager => instance.worldSpaceTextManager;
        public static CameraManager         CameraController    => instance.cameraManager;
        public static DropManager           DropManager         => instance.dropManager;
        public static GameScreenBehavior    GameScreen          => instance.gameScreen;
        // ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Ensure playtime tracker is initialized
            CharacterPlaytimeSystem.Init(GameController.SaveManager);

            instance   = this;
            stageSave  = GameController.SaveManager.GetSave<StageSave>("Stage");

            // fresh‑run reset
            stageSave.ResetStageData = true;
            stageSave.EnemiesKilled  = 0;
            GameController.SaveManager.Save(false);

            if (PlayerBehavior.Player != null)
                cachedCharacter = PlayerBehavior.Player.Data;
        }

        private void Start()
        {
            Stage = database.GetStage(stageSave.SelectedStageId);

            // reset per‑run UI stats once
            if (stageSave.ResetStageData && playerStats != null)
            {
                playerStats.ResetStats();
                stageSave.ResetStageData = false;
                GameController.SaveManager.Save(true);
            }

            // wire up timeline & systems
            director.playableAsset = Stage.Timeline;
            spawner.Init(director);
            experienceManager.Init(testingPreset);
            dropManager.Init();
            fieldManager.Init(Stage, director);
            abilityManager.Init(testingPreset, PlayerBehavior.Player.Data);
            cameraManager.Init(Stage);

            // game events
            PlayerBehavior.Player.onPlayerDied  += OnGameFailed;
            experienceManager.onXpLevelChanged += OnPlayerLevelUp;
            director.stopped                   += TimelineStopped;

            // timeline continuation
            director.time = testingPreset != null
                ? testingPreset.StartTime
                : stageSave.Time;
            director.Play();

            // custom stage music
            if (Stage.UseCustomMusic)
                GameController.ChangeMusic(Stage.MusicName);
        }

        private void OnPlayerLevelUp(int newPlayerLevel)
        {
            Debug.Log($"Player leveled up to {newPlayerLevel}.");
            PlayerBehavior.Player.StartInvincibility(1.5f); // or whatever duration you want

            

            // Award **character** XP each time the **player** levels
            if (cachedCharacter == null && PlayerBehavior.Player != null)
                cachedCharacter = PlayerBehavior.Player.Data;

            if (cachedCharacter != null)
                CharacterLevelSystem.AddMatchResults(cachedCharacter, 1, 0f);
            
            CharacterLevelSystem.AddPlayerLevelResults(cachedCharacter, 1);
        }

        private void TimelineStopped(PlayableDirector _)
        {
            if (!gameObject.activeSelf) return;

            // record playtime
            CharacterPlaytimeSystem.AddTime(cachedCharacter, stageSave.TimeAlive);
            GameController.SaveManager.Save(false);

            // advance stage progression
            int next = stageSave.SelectedStageId + 1;
            if (stageSave.MaxReachedStageId < next && next < database.StagesCount)
                stageSave.SetMaxReachedStageId(next);

            stageSave.IsPlaying = false;
            GameController.SaveManager.Save(true);

            gameScreen.Hide();
            stageCompletedScreen.Show();
            Time.timeScale = 0;
        }

        private void OnGameFailed()
        {
            Time.timeScale = 0;
            stageSave.IsPlaying = false;

            // still record playtime on fail
            CharacterPlaytimeSystem.AddTime(cachedCharacter, stageSave.TimeAlive);
            GameController.SaveManager.Save(true);

            gameScreen.Hide();
            stageFailedScreen.Show();
        }

        public static void ResurrectPlayer()
        {
            EnemiesSpawner.DealDamageToAllEnemies(PlayerBehavior.Player.Damage * 1000);
            GameScreen.Show();
            PlayerBehavior.Player.Revive();
            Time.timeScale = 1;
        }

        public static void ReturnToMainMenu()
        {
            GameController.LoadMainMenu();
        }

        private void OnDisable()
        {
            PlayerBehavior.Player.onPlayerDied     -= OnGameFailed;
            experienceManager.onXpLevelChanged    -= OnPlayerLevelUp;
            director.stopped                      -= TimelineStopped;
        }
    }
}
