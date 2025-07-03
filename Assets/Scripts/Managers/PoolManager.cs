using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PoolManager handles pooling of reusable components to optimize performance.
/// It pre-instantiates components and reuses them instead of creating/destroying frequently.
/// </summary>
[DisallowMultipleComponent]
public class PoolManager : SingletonMonobehaviour<PoolManager>
{
    #region === Serialized Fields ===

    [Tooltip("Populate this array with prefabs to be pooled. Each entry defines the prefab, its pool size, and the type of component to reuse.")]
    [SerializeField] private Pool[] poolArray = null;

    #endregion

    #region === Private Fields ===

    private Transform objectPoolTransform;
    private Dictionary<int, Queue<Component>> poolDictionary = new();

    [Serializable]
    public struct Pool
    {
        public int poolSize;             // Number of objects to pool
        public GameObject prefab;        // Prefab to instantiate
        public string componentType;     // Type of component to reuse (must match script class name)
    }

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        objectPoolTransform = this.transform;

        // Create object pools for all defined prefabs
        for (int i = 0; i < poolArray.Length; i++)
        {
            CreatePool(poolArray[i].prefab, poolArray[i].poolSize, poolArray[i].componentType);
        }
    }

    #endregion

    #region === Pool Creation ===

    /// <summary>
    /// Creates a pool for the specified prefab with the given pool size and component type.
    /// </summary>
    private void CreatePool(GameObject prefab, int poolSize, string componentType)
    {
        int poolKey = prefab.GetInstanceID();
        string prefabName = prefab.name;

        // Create a parent GameObject to organize pooled objects
        GameObject parentObject = new(prefabName + "Anchor");
        parentObject.transform.SetParent(objectPoolTransform);

        if (!poolDictionary.ContainsKey(poolKey))
        {
            poolDictionary.Add(poolKey, new Queue<Component>());

            for (int i = 0; i < poolSize; i++)
            {
                GameObject newObject = Instantiate(prefab, parentObject.transform);
                newObject.SetActive(false);

                Component component = newObject.GetComponent(Type.GetType(componentType));
                poolDictionary[poolKey].Enqueue(component);
            }
        }
    }

    #endregion

    #region === Public API ===

    /// <summary>
    /// Retrieves a pooled component instance for reuse, resetting its transform.
    /// </summary>
    /// <param name="prefab">Prefab used as pool key.</param>
    /// <param name="position">Position to place the reused object.</param>
    /// <param name="rotation">Rotation of the reused object.</param>
    /// <returns>The reused Component if available, otherwise null.</returns>
    public Component ReuseComponent(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int poolKey = prefab.GetInstanceID();

        if (poolDictionary.ContainsKey(poolKey))
        {
            Component reusedComponent = GetComponentFromPool(poolKey);
            ResetObject(reusedComponent, position, rotation, prefab);
            return reusedComponent;
        }

        Debug.LogWarning($"[PoolManager] No pool found for prefab: {prefab.name}");
        return null;
    }

    #endregion

    #region === Internal Helpers ===

    /// <summary>
    /// Dequeues and re-enqueues a component from the pool.
    /// </summary>
    private Component GetComponentFromPool(int poolKey)
    {
        Component component = poolDictionary[poolKey].Dequeue();
        poolDictionary[poolKey].Enqueue(component);

        if (component.gameObject.activeSelf)
            component.gameObject.SetActive(false);

        return component;
    }

    /// <summary>
    /// Resets transform and scale of a reused component.
    /// </summary>
    private void ResetObject(Component component, Vector3 position, Quaternion rotation, GameObject prefab)
    {
        Transform t = component.transform;
        t.position = position;
        t.rotation = rotation;
        t.localScale = prefab.transform.localScale;
    }

    #endregion
}
