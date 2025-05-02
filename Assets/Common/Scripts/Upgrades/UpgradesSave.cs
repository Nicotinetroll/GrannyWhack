using OctoberStudio.Save;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Upgrades
{
    [System.Serializable]
    public class UpgradesSave : ISave
    {
        [SerializeField] UpgradeSave[] savedUpgrades;

        Dictionary<UpgradeType, int> upgradesLevels;

        public event UnityAction<UpgradeType, int> onUpgradeLevelChanged;

        /*────────────────────── Init / API ─────────────────────*/
        public void Init()
        {
            upgradesLevels = new Dictionary<UpgradeType, int>();
            if (savedUpgrades == null) savedUpgrades = new UpgradeSave[0];

            foreach (var s in savedUpgrades)
            {
                if (upgradesLevels.TryGetValue(s.UpgradeType, out int old))
                    upgradesLevels[s.UpgradeType] = Mathf.Max(old, s.Level);
                else
                    upgradesLevels.Add(s.UpgradeType, s.Level);
            }
        }

        public int GetUpgradeLevel(UpgradeType upg) =>
            upgradesLevels.TryGetValue(upg, out int lvl) ? lvl : -1;

        public void SetUpgradeLevel(UpgradeType upg, int lvl)
        {
            upgradesLevels ??= new Dictionary<UpgradeType, int>();
            upgradesLevels[upg] = lvl;
            onUpgradeLevelChanged?.Invoke(upg, lvl);
        }

        public void RemoveUpgrade(UpgradeType upg) => upgradesLevels?.Remove(upg);

        public void Flush()
        {
            upgradesLevels ??= new Dictionary<UpgradeType, int>();

            savedUpgrades = new UpgradeSave[upgradesLevels.Count];
            int i = 0;
            foreach (var kv in upgradesLevels)
                savedUpgrades[i++] = new UpgradeSave(kv.Key, kv.Value);
        }

        /*────────── NEW – hard‑reset helper ─────────*/
        public void ResetAll()
        {
            upgradesLevels?.Clear();
            savedUpgrades = System.Array.Empty<UpgradeSave>();

            // broadcast a generic “changed” so UI knows to redraw
            onUpgradeLevelChanged?.Invoke(UpgradeType.Damage, 0);
            Debug.Log("[UpgradesSave] ResetAll ▶ all upgrade levels wiped");
        }

        /*──────────── nested struct ────────────*/
        [System.Serializable]
        class UpgradeSave
        {
            [SerializeField] UpgradeType upgradeType;
            [SerializeField] int         level;

            public UpgradeType UpgradeType => upgradeType;
            public int         Level       => level;

            public UpgradeSave(UpgradeType t, int l) { upgradeType = t; level = l; }
        }
    }
}
