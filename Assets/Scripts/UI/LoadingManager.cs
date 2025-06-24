using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingManager : SingletonMonobehaviour<LoadingManager>
{
    [Header("UI")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Settings")]
    [SerializeField] private int targetSceneIndex = 1;
    [SerializeField] private float initialDelay = 0.5f;

    private void Start()
    {
        // Thêm outline cho text
        if (progressText != null)
        {
            progressText.outlineWidth = 0.4f;
            progressText.outlineColor = Color.grey;
        }

        StartCoroutine(AutoLoadScene());
    }

    private IEnumerator AutoLoadScene()
    {
        yield return new WaitForSeconds(initialDelay);
        LoadScene(targetSceneIndex);
    }

    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(LoadLevelAsync(sceneIndex));
    }

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

        // Đảm bảo thanh đầy 100%
        loadingBar.value = 1f;
        progressText.text = "남은 시간은...100%";

        // Load scene sau 1 frame
        yield return null;
        SceneManager.LoadScene(sceneIndex);
    }
}
