using System.Collections.Generic;
using UnityEngine;

namespace SatanWagen.VFX
{
    public class ParticlePool : MonoBehaviour
    {
        public static ParticlePool Instance;

        [System.Serializable]
        public class PoolEntry
        {
            public GameObject prefab;
            public int preloadAmount = 10;
        }

        [SerializeField] List<PoolEntry> pools = new List<PoolEntry>();

        private Dictionary<GameObject, Queue<GameObject>> poolDict = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var entry in pools)
            {
                if (!poolDict.ContainsKey(entry.prefab))
                {
                    poolDict[entry.prefab] = new Queue<GameObject>();

                    for (int i = 0; i < entry.preloadAmount; i++)
                    {
                        var go = Instantiate(entry.prefab, transform);
                        go.SetActive(false);
                        poolDict[entry.prefab].Enqueue(go);
                    }
                }
            }
        }

        public void Play(GameObject prefab, Vector3 position, Quaternion? rotation = null)
        {
            if (!poolDict.ContainsKey(prefab))
            {
                poolDict[prefab] = new Queue<GameObject>();
            }

            GameObject go = poolDict[prefab].Count > 0
                ? poolDict[prefab].Dequeue()
                : Instantiate(prefab, transform);

            go.transform.position = position;
            go.transform.rotation = rotation ?? Quaternion.identity;
            go.SetActive(true);

            if (go.TryGetComponent<ParticleSystem>(out var ps))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();

                StartCoroutine(DisableAfterTime(prefab, go, ps.main.duration));
            }
        }

        private System.Collections.IEnumerator DisableAfterTime(GameObject prefab, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
            poolDict[prefab].Enqueue(obj);
        }
    }
}
