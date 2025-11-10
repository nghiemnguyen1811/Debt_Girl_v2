using UnityEngine;

public class PreloadManager : MonoBehaviour
{
    private void OnDestroy()
    {
        // Only release if the AddressablePreloadManager actually exists
        if (AddressablePreloadManager.Instance != null)
        {
            AddressablePreloadManager.Instance.ReleaseAll();
        }
        else
        {
            Debug.LogWarning("[PreloadManager] AddressablePreloadManager not found — skipped ReleaseAll().");
        }
    }
}
