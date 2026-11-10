using System.Collections.Generic;
using UnityEngine;

namespace LightWeaver.Pooling
{
    /// <summary>
    /// Havuzlanan nesnelerin doğma ve havuza geri dönme yaşam döngüsü arayüzü.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnedFromPool();
        void OnReturnedToPool();
    }

    /// <summary>
    /// Unity 6 Mobil standartlarına uygun Sıfır Tahsisli (Zero-GC) merkezi nesne havuzu.
    /// Çalışma anında Instantiate/Destroy çağrılarını ortadan kaldırır.
    /// </summary>
    public sealed class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [System.Serializable]
        public struct PoolDefinition
        {
            public string poolKey;
            public GameObject prefab;
            public int initialCapacity;
        }

        [SerializeField] private List<PoolDefinition> prewarmPools = new List<PoolDefinition>();

        private readonly Dictionary<string, Stack<GameObject>> _pools = new Dictionary<string, Stack<GameObject>>(16);
        private readonly Dictionary<string, GameObject> _prefabLookup = new Dictionary<string, GameObject>(16);
        private readonly Dictionary<int, string> _instanceToKey = new Dictionary<int, string>(128);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePools();
        }

        private void InitializePools()
        {
            for (int i = 0; i < prewarmPools.Count; i++)
            {
                PoolDefinition def = prewarmPools[i];
                if (string.IsNullOrEmpty(def.poolKey) || def.prefab == null) continue;
                RegisterPool(def.poolKey, def.prefab, def.initialCapacity);
            }
        }

        public void RegisterPool(string poolKey, GameObject prefab, int initialCount)
        {
            if (_pools.ContainsKey(poolKey)) return;

            var stack = new Stack<GameObject>(Mathf.Max(4, initialCount));
            _pools[poolKey] = stack;
            _prefabLookup[poolKey] = prefab;

            for (int i = 0; i < initialCount; i++)
            {
                GameObject obj = CreateNewInstance(poolKey, prefab);
                obj.SetActive(false);
                stack.Push(obj);
            }
        }

        public GameObject Spawn(string poolKey, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(poolKey, out Stack<GameObject> stack))
            {
                Debug.LogWarning($"[ObjectPooler] Pool key not registered: {poolKey}");
                return null;
            }

            GameObject instance = null;
            while (stack.Count > 0 && instance == null)
            {
                instance = stack.Pop();
            }

            if (instance == null)
            {
                instance = CreateNewInstance(poolKey, _prefabLookup[poolKey]);
            }

            Transform tr = instance.transform;
            tr.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnSpawnedFromPool();
            }

            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnReturnedToPool();
            }

            instance.SetActive(false);
            instance.transform.SetParent(transform, false);

            int id = instance.GetInstanceID();
            if (_instanceToKey.TryGetValue(id, out string key) && _pools.TryGetValue(key, out Stack<GameObject> stack))
            {
                stack.Push(instance);
            }
        }

        private GameObject CreateNewInstance(string poolKey, GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.name = $"{prefab.name}_Pooled";
            _instanceToKey[obj.GetInstanceID()] = poolKey;
            return obj;
        }
    }
}
