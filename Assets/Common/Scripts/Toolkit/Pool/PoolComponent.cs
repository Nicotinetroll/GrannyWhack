using UnityEngine;

namespace OctoberStudio.Pool
{
    public class PoolComponent<T> : Pool<T> where T : Component
    {
        private GameObject prefab;
        private Transform parent;
        private bool dontDestroyOnLoad;

        public PoolComponent(string name, GameObject prefab, int startingSize, Transform parent = null, bool dontDestroyOnLoad = false) : base(name, startingSize)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.dontDestroyOnLoad = dontDestroyOnLoad;

            for (int i = 0; i <= startingSize; i++)
            {
                AddNewEntity();
            }
        }

        public PoolComponent(GameObject prefab, int startingSize, Transform parent = null, bool dontDestroyOnLoad = false) : base(prefab.name, startingSize)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.dontDestroyOnLoad = dontDestroyOnLoad;

            for (int i = 0; i <= startingSize; i++)
            {
                AddNewEntity();
            }
        }

        protected override T CreateEntity()
        {
            var go = Object.Instantiate(prefab, parent);

            if (dontDestroyOnLoad && parent == null)
            {
                Object.DontDestroyOnLoad(go);
            }

            var component = go.GetComponent<T>();

            if (component == null)
            {
                Debug.LogError($"❌ [PoolComponent<{typeof(T).Name}>] Prefab '{prefab.name}' is missing required component on the root object.");
            }

            return component;
        }

        protected override void InitEntity(T entity)
        {
            if (entity == null)
            {
                Debug.LogWarning($"⚠️ Tried to InitEntity but got null for type {typeof(T).Name}. Skipping.");
                return;
            }

            entity.gameObject.SetActive(false);
        }

        protected override bool ValidateEntity(T entity)
        {
            return entity != null && !entity.gameObject.activeSelf;
        }

        public override T GetEntity()
        {
            var entity = base.GetEntity();

            if (entity != null)
            {
                entity.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"⚠️ [PoolComponent<{typeof(T).Name}>] GetEntity returned null!");
            }

            return entity;
        }

        protected override void DisableEntity(T entity)
        {
            if (entity != null)
            {
                entity.gameObject.SetActive(false);
            }
        }

        protected override void DestroyEntity(T entity)
        {
            if (entity != null)
            {
                Object.Destroy(entity.gameObject);
            }
        }
    }
}
