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
        private CharacterData cachedCharacter;

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
            // Initialize playtime tracker
            CharacterPlaytimeSystem.Init(GameController.SaveManager);

            instance  = this;
            stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");

            // Reset per‑run counters
            stageSave.ResetStageData = true;
            stageSave.EnemiesKilled  = 0;
            GameController.SaveManager.Save(false);

            if (PlayerBehavior.Player != null)
                cachedCharacter = PlayerBehavior.Player.Data;
        }

        private void GrantCharacterExperience()
        {
            if (cachedCharacter == null && PlayerBehavior.Player != null)
                cachedCharacter = PlayerBehavior.Player.Data;

            if (cachedCharacter == null)
            {
                Debug.LogWarning("[StageController] Missing CharacterData – XP not granted.");
                return;
            }

            // 1) Record this run’s play time and flush it right away
            CharacterPlaytimeSystem.AddTime(cachedCharacter, stageSave.TimeAlive);
            GameController.SaveManager.Save(false);

            // 2) Grant XP as before
            float totalDamage = playerStats ? playerStats.TotalDamage : 0f;
            CharacterLevelSystem.AddMatchResults(
                cachedCharacter,
                stageSave.EnemiesKilled,
                totalDamage);
        }

        private void Start()
        {
            Stage = database.GetStage(stageSave.SelectedStageId);

            // Reset stats on new run
            if (stageSave.ResetStageData && playerStats != null)
            {
                playerStats.ResetStats();
                stageSave.ResetStageData = false;
                GameController.SaveManager.Save(true);
            }

            director.playableAsset = Stage.Timeline;

            spawner.Init(director);
            experienceManager.Init(testingPreset);
            dropManager.Init();
            fieldManager.Init(Stage, director);
            abilityManager.Init(testingPreset, PlayerBehavior.Player.Data);
            cameraManager.Init(Stage);

            PlayerBehavior.Player.onPlayerDied    += OnGameFailed;
            experienceManager.onXpLevelChanged  += OnPlayerLevelUp;
            GameScreen.AbilitiesWindow.onPanelClosed += () =>
            {
                PlayerBehavior.Player.StartInvincibility(1f);
            };

            director.stopped += TimelineStopped;

            // Rewind timeline if needed
            if (testingPreset != null)
            {
                director.time = testingPreset.StartTime;
            }
            else
            {
                double time = stageSave.Time;
                var clips = director.GetClips<BossTrack, Boss>();
                foreach (var clip in clips)
                {
                    if (time >= clip.start && time <= clip.end)
                    {
                        time = clip.start;
                        break;
                    }
                }
                director.time = time;
            }

            director.Play();

            if (Stage.UseCustomMusic)
                GameController.ChangeMusic(Stage.MusicName);
        }

        private void OnPlayerLevelUp(int level)
        {
            Debug.Log($"Player leveled up to {level}.");
        }

        private void TimelineStopped(PlayableDirector _)
        {
            if (!gameObject.activeSelf) return;

            GrantCharacterExperience();

            // Update max reached stage
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
