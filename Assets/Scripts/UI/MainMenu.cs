using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject instructPanel;
    [SerializeField] private GameObject quitPanel;
    [SerializeField] private ScenePreloadDataSO gameplayPreloadDataSO;
    private void Start()
    {
        AudioManager.Instance.PlayMusic(0);
    }

    public void StartGame()
    {
        SceneLoadRequest.DataToPreload = gameplayPreloadDataSO;
        SceneManager.LoadScene(1);
    }

    public void ToggleSettingsPanel(bool show) => TogglePanel(settingsPanel, show);

    public void ToggleInstructPanel(bool show) => TogglePanel(instructPanel, show);

    public void ToggleQuitPanel(bool show) => TogglePanel(quitPanel, show);

    private void TogglePanel(GameObject panel, bool show)
    {
        if (panel == null) return;
        panel.SetActive(show);

        AudioManager.Instance.PlayInteractSound(8);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}