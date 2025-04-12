using OctoberStudio.UI;
using UnityEngine;

namespace OctoberStudio.Buffs
{
    public class BuffManager : MonoBehaviour
    {
        public static BuffManager Instance { get; private set; }

        [SerializeField] private BuffsDatabase buffsDatabase;
        [SerializeField] private BuffUI buffUI; // ✅ Make sure this is here

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public BuffData GetBuff(BuffType type)
        {
            if (buffsDatabase == null)
            {
                Debug.LogError("BuffsDatabase is not assigned in BuffManager!");
                return null;
            }

            return buffsDatabase.GetBuff(type);
        }

        public void ApplyBuff(BuffType type)
        {
            BuffData buff = GetBuff(type);
            if (buff == null || buff.RuntimePrefab == null)
            {
                Debug.LogWarning($"Buff or prefab not found for {type}");
                return;
            }

            var instance = Instantiate(buff.RuntimePrefab, transform);
            instance.Init(buff);

            // ✅ Show in UI
            if (buffUI != null)
            {
                buffUI.ShowBuff(buff.Title, buff.Duration);
            }
        }
    }
}