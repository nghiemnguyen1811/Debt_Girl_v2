using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Handles checking and downloading Addressables content updates (remote catalog + bundles).
/// </summary>
public class RemoteContentUpdater : MonoBehaviour
{
    public bool HasUpdate { get; private set; }
    public bool IsUpdating { get; private set; }

    /// <summary>
    /// Checks if a newer Addressables catalog is available on the server.
    /// </summary>
    public IEnumerator CheckForUpdate()
    {
        HasUpdate = false;

        var checkHandle = Addressables.CheckForCatalogUpdates();
        yield return checkHandle;

        if (checkHandle.Status == AsyncOperationStatus.Succeeded &&
            checkHandle.Result != null &&
            checkHandle.Result.Count > 0)
        {
            HasUpdate = true;
            Debug.Log("Hot Update: New catalog detected.");
        }
        else
        {
            Debug.Log("Hot Update: No updates available.");
        }
    }

    /// <summary>
    /// Downloads the updated catalog and its required AssetBundles.
    /// </summary>
    public IEnumerator DownloadUpdate(Action<float> onProgress = null)
    {
        if (!HasUpdate)
            yield break;

        IsUpdating = true;

        var checkHandle = Addressables.CheckForCatalogUpdates();
        yield return checkHandle;

        var catalogsToUpdate = checkHandle.Result;
        if (catalogsToUpdate == null || catalogsToUpdate.Count == 0)
        {
            IsUpdating = false;
            yield break;
        }

        // Download updated catalogs and bundles
        var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate);

        while (!updateHandle.IsDone)
        {
            onProgress?.Invoke(updateHandle.PercentComplete);
            yield return null;
        }

        yield return updateHandle;

        Debug.Log("Hot Update: Content updated successfully.");
        IsUpdating = false;
    }
}
