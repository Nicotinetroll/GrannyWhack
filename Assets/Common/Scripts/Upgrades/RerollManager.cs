using UnityEngine;
using OctoberStudio.Currency;

namespace OctoberStudio.Systems
{
    /// <summary>Persistent, unlimited-stack currency for ability re-rolls.</summary>
    public sealed class RerollManager : MonoBehaviour
    {
        public static RerollManager Instance { get; private set; }

        const string REROLL_ID = "Reroll";   // must exist in Currencies Database + SaveManager
        const string GOLD_ID   = "gold";     // your Gold save key (lower-case matches codebase)

        [Header("Prices")]
        [SerializeField] int inRunPrice  = 500;   // when level-up popup is open
        [SerializeField] int menuPrice   = 100;   // in the Upgrades window
        [SerializeField] int defaultStackOnFreshSave = 0;

        CurrencySave rerollSave;   // “Reroll” balance
        CurrencySave goldSave;     // “gold”   balance

        public int  Stack      => rerollSave?.Amount ?? 0;
        public int  InRunPrice => inRunPrice;
        public int  MenuPrice  => menuPrice;

        public static event System.Action<int> OnStackChanged;

        /* ───────── Unity ───────── */
        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            rerollSave = GameController.SaveManager.GetSave<CurrencySave>(REROLL_ID);
            goldSave   = GameController.SaveManager.GetSave<CurrencySave>(GOLD_ID);

            if (defaultStackOnFreshSave > 0 && rerollSave.Amount < defaultStackOnFreshSave)
                rerollSave.Deposit(defaultStackOnFreshSave - rerollSave.Amount);

            OnStackChanged?.Invoke(Stack);
        }

        /* ───────── API ───────── */
        public bool TryConsume()
        {
            if (!rerollSave.TryWithdraw(1)) return false;
            Sync();
            return true;
        }

        public bool TryBuyInRun()  => Buy(inRunPrice);
        public bool TryBuyInMenu() => Buy(menuPrice);

        /* ───────── helpers ───── */
        bool Buy(int price)
        {
            if (!goldSave.TryWithdraw(price)) return false;

            rerollSave.Deposit(1);
            Sync();
            return true;
        }

        void Sync()
        {
            OnStackChanged?.Invoke(Stack);
            GameController.SaveManager.Save();
        }
    }
}
