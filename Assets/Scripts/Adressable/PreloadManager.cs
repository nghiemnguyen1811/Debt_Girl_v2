using UnityEngine;

public class PreloadManager : MonoBehaviour
{
    private void OnDestroy()
    {
        AddressablePreloadManager.Instance.ReleaseAll();
    }
}
