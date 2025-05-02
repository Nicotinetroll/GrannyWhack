/************************************************************
 *  RerollManager.cs – persistent currency & logic for re‑rolls
 ************************************************************/
using System;
using UnityEngine;
using OctoberStudio.Currency;

namespace OctoberStudio.Systems
{
    /// <summary>Persistent, unlimited‑stack currency for ability re‑rolls.</summary>
    public sealed class RerollManager : MonoBehaviour
    {
        public static RerollManager Instance { get; private set; }

        const string REROLL_ID = "Reroll";   // must exist in Currencies DB + SaveManager
        const string GOLD_ID   = "gold";     // universal Gold key (lower‑case matches codebase)

        [Header("Prices")]
        [SerializeField] int inRunPrice                = 500;
        [SerializeField] int menuPrice                 = 100;
        [SerializeField] int defaultStackOnFreshSave   = 0;

        CurrencySave rerollSave;   // “Reroll” balance
        CurrencySave goldSave;     // “gold”   balance

        /*──────────── public API ────────────*/
        public int  Stack      => rerollSave?.Amount ?? 0;
        public int  InRunPrice => inRunPrice;
        public int  MenuPrice  => menuPrice;

        public static event Action<int> OnStackChanged;

        /*──────────────── Unity ───────────────*/
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

            /* give starter stack on completely fresh saves */
            if (defaultStackOnFreshSave > 0 && rerollSave.Amount < defaultStackOnFreshSave)
                rerollSave.Deposit(defaultStackOnFreshSave - rerollSave.Amount);

            OnStackChanged?.Invoke(Stack);
        }

        /*──────────── re‑roll consumption / purchase ────────────*/
        public bool TryConsume()
        {
            if (!rerollSave.TryWithdraw(1)) return false;
            Sync();
            return true;
        }

        public bool TryBuyInRun()  => Buy(inRunPrice);
        public bool TryBuyInMenu() => Buy(menuPrice);

        bool Buy(int price)
        {
            if (!goldSave.TryWithdraw(price)) return false;

            rerollSave.Deposit(1);
            Sync();
            return true;
        }

        /*──────────── persistence helper ────────────*/
        void Sync()
        {
            OnStackChanged?.Invoke(Stack);
            GameController.SaveManager.Save();
        }

        /*──────────────── HARD RESET (Dev‑popup) ───────────────*/
        /// <summary>
        /// Clears BOTH “Reroll” and “gold” balances and restores
        /// the manager’s internal fields to their defaults.
        /// </summary>
        public void ResetAll()
        {
            /* 1. zero the two currency saves (if they already exist) */
            if (rerollSave != null) rerollSave.ResetAll();
            if (goldSave   != null) goldSave  .ResetAll();

            /* 2. purge internal stack & notify UI */
            OnStackChanged?.Invoke(0);

            /* 3. wipe *this* instance’s value‑type fields to pristine defaults */
            inRunPrice              = 500;
            menuPrice               = 100;
            defaultStackOnFreshSave = 0;
        }
    }
}
