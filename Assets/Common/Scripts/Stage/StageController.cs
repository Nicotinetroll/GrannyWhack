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
        [SerializeField] PlayerStatsManager playerStats; // ✅ Reference to new system

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

            if (playerStats == null)
                playerStats = Object.FindFirstObjectByType<PlayerStatsManager>();
        }

        private void Start()
        {
            Stage = database.GetStage(stageSave.SelectedStageId);

            // ✅ Reset PlayerStatsManager only on new game
            if (stageSave.ResetStageData && playerStats != null)
            {
                playerStats.ResetStats();
                stageSave.ResetStageData = false;
                GameController.SaveManager.Save(true); // Save the cleared flag
            }

            director.playableAsset = Stage.Timeline;

            spawner.Init(director);
            experienceManager.Init(testingPreset);
            dropManager.Init();
            fieldManager.Init(Stage, director);
            abilityManager.Init(testingPreset, PlayerBehavior.Player.Data);
            cameraManager.Init(Stage);

            PlayerBehavior.Player.onPlayerDied += OnGameFailed;
            experienceManager.onXpLevelChanged += OnPlayerLevelUp;

            GameScreen.AbilitiesWindow.onPanelClosed += () =>
            {
                PlayerBehavior.Player.StartInvincibility(1f);
            };

            director.stopped += TimelineStopped;

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
        }

        private void OnPlayerLevelUp(int level)
        {
            Debug.Log($"Player leveled up to {level}.");
        }

        private void TimelineStopped(PlayableDirector director)
        {
            if (!gameObject.activeSelf) return;

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

        // ❌ No need for Update() for damage anymore — handled in PlayerStatsManager!
    }
}
