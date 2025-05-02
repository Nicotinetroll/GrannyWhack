using OctoberStudio.Save;
using System;
using UnityEngine;

namespace OctoberStudio
{
    public class CurrencySave : ISave
    {
        [SerializeField] int amount;
        public int Amount => amount;

        public event Action<int> onGoldAmountChanged;

        /*────────────────────── API ─────────────────────*/
        public void Deposit(int depositedAmount)
        {
            amount += depositedAmount;
            onGoldAmountChanged?.Invoke(amount);
        }

        public void Withdraw(int withdrawnAmount)
        {
            amount = Mathf.Max(0, amount - withdrawnAmount);
            onGoldAmountChanged?.Invoke(amount);
        }

        public bool TryWithdraw(int withdrawnAmount)
        {
            if (amount < withdrawnAmount) return false;
            amount -= withdrawnAmount;
            onGoldAmountChanged?.Invoke(amount);
            return true;
        }

        public bool CanAfford(int cost) => amount >= cost;

        public void Flush() { }               // nothing special

        /*────────── NEW – hard‑reset helper ─────────*/
        public void ResetAll()
        {
            amount = 0;
            onGoldAmountChanged?.Invoke(amount);
            Debug.Log("[CurrencySave] ResetAll ▶ Gold = 0");
        }
    }
}