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
        public static StageData Stage { get; private set; }

        private StageSave stageSave;

        private void Awake()
        {
            instance = this;
            stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
            CharacterLevelSystem.Init(GameController.SaveManager);

            // Ensure PlayerStatsManager is found
            if (playerStats == null)
                playerStats = Object.FindFirstObjectByType<PlayerStatsManager>();
        }
        
        private void GrantCharacterExperience()
        {
            // grab the damage the UI has been aggregating the whole run
            float totalDamage = PlayerStatsManager.Instance
                ? PlayerStatsManager.Instance.TotalDamage
                : 0f;   // failsafe – won’t crash in weird test scenes

            CharacterLevelSystem.AddMatchResults(
                PlayerBehavior.Player.Data,
                stageSave.EnemiesKilled,
                totalDamage);
        }


        private void Start()
        {
            Stage = database.GetStage(stageSave.SelectedStageId);

            // Reset player stats if this is a new run
            if (stageSave.ResetStageData && playerStats != null)
            {
                playerStats.ResetStats();
                stageSave.ResetStageData = false;
                GameController.SaveManager.Save(true);
            }

            // Load stage timeline
            director.playableAsset = Stage.Timeline;

            // Init systems
            spawner.Init(director);
            experienceManager.Init(testingPreset);
            dropManager.Init();
            fieldManager.Init(Stage, director);
            abilityManager.Init(testingPreset, PlayerBehavior.Player.Data);
            cameraManager.Init(Stage);

            PlayerBehavior.Player.onPlayerDied += OnGameFailed;
            experienceManager.onXpLevelChanged += OnPlayerLevelUp;

            // 🛡️ Invincibility after closing ability panel
            GameScreen.AbilitiesWindow.onPanelClosed += () =>
            {
                PlayerBehavior.Player.StartInvincibility(1f);
            };

            director.stopped += TimelineStopped;

            // Rewind time if needed (based on saved state or preset)
            if (testingPreset != null)
            {
                director.time = testingPreset.StartTime;
            }
            else
            {
                var time = stageSave.Time;
                var bossClips = director.GetClips<BossTrack, Boss>();

                foreach (var bossClip in bossClips)
                {
                    if (time >= bossClip.start && time <= bossClip.end)
                    {
                        time = (float)bossClip.start;
                        break;
                    }
                }

                director.time = time;
            }

            director.Play();

            // 🎵 Set custom music
            if (Stage.UseCustomMusic)
            {
                GameController.ChangeMusic(Stage.MusicName);
            }
        }

        private void OnPlayerLevelUp(int level)
        {
            Debug.Log($"Player leveled up to {level}.");
        }

        private void TimelineStopped(PlayableDirector director)
        {
            if (!gameObject.activeSelf) return;
            
            GrantCharacterExperience();

            if (stageSave.MaxReachedStageId < stageSave.SelectedStageId + 1 &&
                stageSave.SelectedStageId + 1 < database.StagesCount)
            {
                stageSave.SetMaxReachedStageId(stageSave.SelectedStageId + 1);
            }

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
            
            GrantCharacterExperience();   
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
            director.stopped -= TimelineStopped;
        }
    }
}
