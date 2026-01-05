using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Manages preload and release of Addressable assets for each scene.
/// Attach this to a GameObject in your MainMenu or InitScene.
/// </summary>
public class AddressablePreloadManager : SingletonMonobehaviour<AddressablePreloadManager>
{
    //─────────────────────────────────────────────────────────────
    #region Runtime Data

    private readonly Dictionary<string, AsyncOperationHandle> loadedHandles = new();

    public bool IsInitialized { get; private set; } = false;
    public bool InitFailed { get; private set; } = false;

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region Initialization

    private IEnumerator Start()
    {
        Debug.Log("[AddressablePreloadManager] Initializing Addressables...");

        // Start init
        var initHandle = Addressables.InitializeAsync();

        // Wait for completion
        yield return initHandle;

        // IMPORTANT: handle may be invalid in some failure cases; do NOT touch initHandle.Status directly.
        bool valid = initHandle.IsValid();
        var ex = valid ? initHandle.OperationException : null;

        if (ex == null)
        {
            IsInitialized = true;
            InitFailed = false;
            Debug.Log("[AddressablePreloadManager] Addressables init succeeded.");
        }
        else
        {
            IsInitialized = false;
            InitFailed = true;
            Debug.LogError("[AddressablePreloadManager] Addressables init FAILED: " + ex);
        }

        // Release init handle to avoid holding resources
        if (valid)
            Addressables.Release(initHandle);
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region Preloading

    /// <summary>
    /// Preload all addressable assets listed in the preloadData.
    /// </summary>
    public IEnumerator Preload(ScenePreloadDataSO preloadData)
    {
        // Wait for init to complete (or fail)
        while (!IsInitialized && !InitFailed)
            yield return null;

        if (InitFailed)
        {
            Debug.LogWarning("[AddressablePreloadManager] Preload skipped because Addressables init failed.");
            yield break;
        }

        if (preloadData == null || preloadData.addressableKeysToPreload == null)
            yield break;

        loadedHandles.Clear();

        foreach (var key in preloadData.addressableKeysToPreload)
        {
            if (string.IsNullOrEmpty(key))
                continue;

            var handle = Addressables.LoadAssetAsync<Object>(key);
            yield return handle;

            // Same rule: don't touch Status if handle invalid
            if (handle.IsValid() && handle.OperationException == null)
            {
                loadedHandles[key] = handle;
                Debug.Log("[AddressablePreloadManager] Preloaded asset: " + key);
            }
            else
            {
                Debug.LogWarning("[AddressablePreloadManager] Failed to preload: " + key +
                                 (handle.IsValid() ? (" | " + handle.OperationException) : " | invalid handle"));
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region Accessor

    public T GetPreloadedAsset<T>(string key) where T : Object
    {
        if (loadedHandles.TryGetValue(key, out var handle) && handle.IsValid())
            return handle.Result as T;

        return null;
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region Cleanup

    public void ReleaseAll()
    {
        foreach (var handle in loadedHandles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        loadedHandles.Clear();
        Debug.Log("[AddressablePreloadManager] Released all preloaded Addressables.");
    }

    #endregion
    //─────────────────────────────────────────────────────────────
}
