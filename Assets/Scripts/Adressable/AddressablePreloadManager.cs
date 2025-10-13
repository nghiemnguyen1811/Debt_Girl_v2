using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Manages preload and release of Addressable assets for each scene.
/// Attach this to a GameObject in your MainMenu or InitScene.
/// </summary>
public class AddressablePreloadManager : MonoBehaviour
{
    #region Singleton Setup

    public static AddressablePreloadManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
            Debug.LogWarning("AddressablePreloadManager is not a root GameObject. DontDestroyOnLoad will be ignored.");

        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Runtime Data

    private readonly Dictionary<string, AsyncOperationHandle> loadedHandles = new();

    #endregion

    #region Preloading

    /// <summary>
    /// Preload all addressable assets listed in the preloadData.
    /// </summary>
    public IEnumerator Preload(ScenePreloadDataSO preloadData)
    {
        if (preloadData == null || preloadData.addressableKeysToPreload == null)
            yield break;

        loadedHandles.Clear();

        foreach (var key in preloadData.addressableKeysToPreload)
        {
            if (string.IsNullOrEmpty(key)) continue;

            var handle = Addressables.LoadAssetAsync<Object>(key);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedHandles[key] = handle;
                Debug.Log("Preloaded asset: " + key);
            }
            else
            {
                Debug.LogWarning("Failed to preload: " + key);
            }
        }
    }

    #endregion

    #region Accessor

    /// <summary>
    /// Get preloaded asset by key (used for spawning).
    /// </summary>
    public T GetPreloadedAsset<T>(string key) where T : Object
    {
        if (loadedHandles.TryGetValue(key, out var handle))
            return handle.Result as T;
        return null;
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Release all cached addressable handles (called after scene exit).
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var handle in loadedHandles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        loadedHandles.Clear();
        Debug.Log("Released all preloaded Addressables.");
    }

    #endregion
}
