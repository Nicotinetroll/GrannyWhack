using OctoberStudio.UI;
using UnityEngine;

namespace OctoberStudio.Buffs
{
    public class BuffManager : MonoBehaviour
    {
        public static BuffManager Instance { get; private set; }

        [SerializeField] private BuffsDatabase buffsDatabase;

        [Header("UI")]
        [SerializeField] private BuffUI buffUIPrefab;             // ✅ Prefab of the BuffUI
        [SerializeField] private Transform buffUIContainer;       // ✅ The "Buffs" GameObject under Canvas

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

            // Apply the actual buff logic to the player/enemy/etc.
            var instance = Instantiate(buff.RuntimePrefab, transform);
            instance.Init(buff);

            // ✅ Create visual representation in UI
            if (buffUIPrefab != null && buffUIContainer != null)
            {
                var uiInstance = Instantiate(buffUIPrefab, buffUIContainer);
                uiInstance.ShowBuff(buff.Title, buff.Duration);
            }
        }
    }
}