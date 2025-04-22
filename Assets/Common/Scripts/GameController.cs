using System.Collections;
using OctoberStudio.Currency;
using OctoberStudio.Upgrades;
using OctoberStudio.Vibration;
using OctoberStudio.Save;
using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OctoberStudio
{
    public class GameController : MonoBehaviour
    {
        private static GameController instance;

        [SerializeField] CurrenciesManager currenciesManager;
        public static CurrenciesManager CurrenciesManager => instance.currenciesManager;

        [SerializeField] UpgradesManager upgradesManager;
        public static UpgradesManager UpgradesManager => instance.upgradesManager;

        public static ISaveManager SaveManager { get; private set; }
        public static IAudioManager AudioManager { get; private set; }
        public static IVibrationManager VibrationManager { get; private set; }
        public static IInputManager InputManager { get; private set; }

        public static CurrencySave Gold { get; private set; }
        public static CurrencySave TempGold { get; private set; }

        /// <summary>
        /// The currently playing music AudioSource (if any).
        /// </summary>
        public static AudioSource Music { get; private set; }

        private static StageSave stageSave;

        // Indicates that the main menu is just loaded (not returning from a stage)
        public static bool FirstTimeLoaded { get; private set; }

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                FirstTimeLoaded = false;
                return;
            }

            instance = this;
            FirstTimeLoaded = true;

            currenciesManager.Init();
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 120;
        }

        private void Start()
        {
            // register all our Save blobs
            SaveManager = SaveManager ?? SaveManager;
            Gold      = SaveManager.GetSave<CurrencySave>("gold");
            TempGold  = SaveManager.GetSave<CurrencySave>("temp_gold");
            stageSave = SaveManager.GetSave<StageSave>("Stage");

            // first‐time stage flag
            if (!stageSave.loadedBefore)
            {
                stageSave.loadedBefore = true;
                // Gold.Deposit(1000);
            }

            // play your default menu music after a tiny delay
            EasingManager.DoAfter(0.1f, () => {
                ChangeMusic("MenuTheme");   // ← set your menu track name here
            });
        }

        public static void RegisterInputManager(IInputManager im)   => InputManager = im;
        public static void RegisterSaveManager(ISaveManager sm)     => SaveManager = sm;
        public static void RegisterVibrationManager(IVibrationManager vm) => VibrationManager = vm;
        public static void RegisterAudioManager(IAudioManager am)   => AudioManager = am;

        /// <summary>Load the game scene (stage).</summary>
        public static void LoadStage()
        {
            if (stageSave.ResetStageData) TempGold.Withdraw(TempGold.Amount);
            instance.StartCoroutine(StageLoadingCoroutine());
            SaveManager.Save(false);
        }

        /// <summary>Return to the main menu, stop stage music, play menu theme.</summary>
        public static void LoadMainMenu()
        {
            // bring over any temp gold
            Gold.Deposit(TempGold.Amount);
            TempGold.Withdraw(TempGold.Amount);

            // stop any stage music so it doesn't bleed into the menu
            StopMusic();
            // now play menu theme
            ChangeMusic("MenuTheme");

            if (instance != null) instance.StartCoroutine(MainMenuLoadingCoroutine());
            SaveManager.Save(false);
        }

        private static IEnumerator StageLoadingCoroutine()
        {
            yield return LoadAsyncScene("Loading Screen", LoadSceneMode.Additive);
            yield return UnloadAsyncScene("Main Menu");
            yield return LoadAsyncScene("Game", LoadSceneMode.Single);
        }

        private static IEnumerator MainMenuLoadingCoroutine()
        {
            yield return LoadAsyncScene("Loading Screen", LoadSceneMode.Additive);
            yield return UnloadAsyncScene("Game");
            yield return LoadAsyncScene("Main Menu", LoadSceneMode.Single);
        }

        private static IEnumerator UnloadAsyncScene(string sceneName)
        {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            op.allowSceneActivation = false;
            while (!op.isDone) yield return null;
        }

        private static IEnumerator LoadAsyncScene(string sceneName, LoadSceneMode mode)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            op.allowSceneActivation = false;
            while (!op.isDone)
            {
                if (op.progress >= 0.9f)
                    op.allowSceneActivation = true;
                yield return null;
            }
        }

        private void OnApplicationFocus(bool focus)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (focus && Music != null && !Music.isPlaying)
                Music = AudioManager.AudioDatabase.Music.Play(true);
#endif
        }

        // ─── New Music Helpers ─────────────────────────────────────────

        /// <summary>
        /// Immediately stops any existing music, then starts the named track.
        /// </summary>
        public static void ChangeMusic(string musicName)
        {
            if (Music != null)
            {
                Music.Stop();
                Music = null;
            }

            if (!string.IsNullOrEmpty(musicName) && AudioManager != null)
            {
                int h = musicName.GetHashCode();
                Music = AudioManager.PlayMusic(h);
            }
        }

        /// <summary>Stop whatever track is playing right now.</summary>
        public static void StopMusic()
        {
            if (Music != null)
            {
                Music.Stop();
                Music = null;
            }
        }
    }
}
