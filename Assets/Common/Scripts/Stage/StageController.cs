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

        [SerializeField] StagesDatabase database;
        [SerializeField] PlayableDirector director;
        [SerializeField] EnemiesSpawner spawner;
        [SerializeField] StageFieldManager fieldManager;
        [SerializeField] ExperienceManager experienceManager;
        [SerializeField] DropManager dropManager;
        [SerializeField] AbilityManager abilityManager;
        [SerializeField] PoolsManager poolsManager;
        [SerializeField] WorldSpaceTextManager worldSpaceTextManager;
        [SerializeField] CameraManager cameraManager;

        [Header("UI")]
        [SerializeField] GameScreenBehavior gameScreen;
        [SerializeField] StageFailedScreen stageFailedScreen;
        [SerializeField] StageCompleteScreen stageCompletedScreen;
        [SerializeField] PlayerStatsManager playerStats;

        [Header("Testing")]
        [SerializeField] PresetData testingPreset;

        private StageSave stageSave;
        private CharacterData cachedCharacter;

        public static StageData Stage { get; private set; }
        public static EnemiesSpawner EnemiesSpawner => instance.spawner;
        public static ExperienceManager ExperienceManager => instance.experienceManager;
        public static AbilityManager AbilityManager => instance.abilityManager;
        public static StageFieldManager FieldManager => instance.fieldManager;
        public static PlayableDirector Director => instance.director;
        public static PoolsManager PoolsManager => instance.poolsManager;
        public static WorldSpaceTextManager WorldSpaceTextManager => instance.worldSpaceTextManager;
        public static CameraManager CameraController => instance.cameraManager;
        public static DropManager DropManager => instance.dropManager;
        public static GameScreenBehavior GameScreen => instance.gameScreen;

        private void Awake()
        {
            instance      = this;
            stageSave     = GameController.SaveManager.GetSave<StageSave>("Stage");
            CharacterPlaytimeSystem.Init(GameController.SaveManager);

            // fresh run
            stageSave.ResetStageData = true;
            stageSave.EnemiesKilled  = 0;
            GameController.SaveManager.Save(false);

            if (PlayerBehavior.Player != null)
                cachedCharacter = PlayerBehavior.Player.Data;
        }

        private void Start()
        {
            Stage = database.GetStage(stageSave.SelectedStageId);

            // reset per‑run stats
            if (stageSave.ResetStageData && playerStats != null)
            {
                playerStats.ResetStats();
                stageSave.ResetStageData = false;
                GameController.SaveManager.Save(true);
            }

            // load timeline
            director.playableAsset = Stage.Timeline;

            // init all systems
            spawner.Init(director);
            experienceManager.Init(testingPreset);
            dropManager.Init();
            fieldManager.Init(Stage, director);
            abilityManager.Init(testingPreset, PlayerBehavior.Player.Data);
            cameraManager.Init(Stage);

            PlayerBehavior.Player.onPlayerDied        += OnGameFailed;
            experienceManager.onXpLevelChanged       += _ => { /* UI level‑up UI if desired */ };
            director.stopped                         += TimelineStopped;

            // rewind if continuing
            if (testingPreset != null)
                director.time = testingPreset.StartTime;
            else
                director.time = stageSave.Time;

            director.Play();

            // ─── Random Stage Music ────────────────────────────
            if (Stage.MusicNames != null && Stage.MusicNames.Count > 0)
            {
                var list = Stage.MusicNames;
                var pick = list[Random.Range(0, list.Count)];
                GameController.ChangeMusic(pick);
            }
            else if (Stage.UseCustomMusic) // legacy fallback
            {
                GameController.ChangeMusic(Stage.MusicName);
            }
        }

        private void TimelineStopped(PlayableDirector dir)
        {
            GrantCharacterExperience();

            if (stageSave.MaxReachedStageId < stageSave.SelectedStageId + 1)
                stageSave.SetMaxReachedStageId(stageSave.SelectedStageId + 1);

            stageSave.IsPlaying = false;
            GameController.SaveManager.Save(true);

            gameScreen.Hide();
            stageCompletedScreen.Show();
            Time.timeScale = 0;
        }

        private void OnGameFailed()
        {
            Time.timeScale = 0;
            GrantCharacterExperience();
            stageSave.IsPlaying = false;
            GameController.SaveManager.Save(true);

            gameScreen.Hide();
            stageFailedScreen.Show();
        }

        private void GrantCharacterExperience()
        {
            if (cachedCharacter == null && PlayerBehavior.Player != null)
                cachedCharacter = PlayerBehavior.Player.Data;
            if (cachedCharacter == null) return;

            float totalDamage = playerStats != null ? playerStats.TotalDamage : 0f;
            CharacterLevelSystem.AddMatchResults(cachedCharacter, stageSave.EnemiesKilled, totalDamage);
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
            // stop any stage music immediately
            GameController.StopMusic();
            GameController.LoadMainMenu();
        }

        private void OnDisable()
        {
            director.stopped -= TimelineStopped;
        }
    }
}
