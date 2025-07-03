using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles loading screen visuals and automatically transitions to the target scene after a delay.
/// </summary>
public class LoadingManager : SingletonMonobehaviour<LoadingManager>
{
    #region === UI References ===

    [Header("UI")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI progressText;

    #endregion

    #region === Settings ===

    [Header("Settings")]
    [SerializeField] private int targetSceneIndex = 1;
    [SerializeField] private float initialDelay = 0.5f;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        // Apply outline to the progress text for better visibility
        if (progressText != null)
        {
            progressText.outlineWidth = 0.4f;
            progressText.outlineColor = Color.grey;
        }

        StartCoroutine(AutoLoadScene());
    }

    #endregion

    #region === Scene Loading Logic ===

    /// <summary>
    /// Wait for an initial delay then start loading the scene.
    /// </summary>
    private IEnumerator AutoLoadScene()
    {
        yield return new WaitForSeconds(initialDelay);
        LoadScene(targetSceneIndex);
    }

    /// <summary>
    /// Public method to initiate scene loading manually.
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(LoadLevelAsync(sceneIndex));
    }

    /// <summary>
    /// Simulates loading progress before actually loading the scene.
    /// </summary>
    private IEnumerator LoadLevelAsync(int sceneIndex)
    {
        float duration = 5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            loadingBar.value = progress;
            progressText.text = "남은 시간은..." + (progress * 100f).ToString("F0") + "%";
            yield return null;
        }

        // Ensure bar is full and text is accurate
        loadingBar.value = 1f;
        progressText.text = "남은 시간은...100%";

        // Load the next scene on the next frame
        yield return null;
        SceneManager.LoadScene(sceneIndex);
    }

    #endregion
}
