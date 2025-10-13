using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles loading flow and optional addressable asset preloading before entering a scene.
/// Attach this in the Loading Scene.
/// </summary>
public class LoadingManager : MonoBehaviour
{
    #region === UI References ===

    [Header("UI Elements")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI progressText;

    #endregion

    #region === Delay Settings ===

    [Header("Delay Settings")]
    [SerializeField] private float initialDelay = 0.5f;

    #endregion

    #region === Unity Lifecycle ===

    private void Start()
    {
        //AudioManager.Instance?.StopMusic();
        StartCoroutine(HandleLoadingFlow());
    }

    #endregion

    #region === Loading Flow ===

    /// <summary>
    /// Main coroutine to control preload and scene loading steps.
    /// </summary>
    private IEnumerator HandleLoadingFlow()
    {
        yield return new WaitForSeconds(initialDelay);

        // Step 1: Get preload data (if any)
        ScenePreloadDataSO preloadData = SceneLoadRequest.DataToPreload;

        // Step 2: If preload is required, do it
        if (preloadData != null && preloadData.addressableKeysToPreload != null && preloadData.addressableKeysToPreload.Length > 0)
        {
            yield return StartCoroutine(AddressablePreloadManager.Instance.Preload(preloadData));
        }

        // Step 3: Load the target scene
        int targetSceneIndex = preloadData != null ? preloadData.sceneIndex : SceneManager.GetActiveScene().buildIndex + 1;
        yield return StartCoroutine(LoadSceneAsync(targetSceneIndex));
    }

    #endregion

    #region === Scene Loading ===

    /// <summary>
    /// Handles async scene loading with UI progress update.
    /// </summary>
    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            loadingBar.value = progress;
            progressText.text = $"{progress * 100f:0}%";

            if (asyncLoad.progress >= 0.9f)
            {
                loadingBar.value = 1f;
                progressText.text = "100%";

                yield return new WaitForSeconds(0.3f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    #endregion
}
