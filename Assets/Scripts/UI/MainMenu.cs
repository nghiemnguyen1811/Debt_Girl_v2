using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls main menu UI, story transitions, preload logic, and general navigation.
/// </summary>
public class MainMenu : SingletonMonobehaviour<MainMenu>
{
    //────────────────────────────────────────────────────
    // == Inspector Fields ==
    //────────────────────────────────────────────────────

    [Header("Menu Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject instructPanel;
    [SerializeField] private GameObject quitPanel;

    [Header("Story UI")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private Fader fader;

    [Header("Preload Data")]
    [SerializeField] private ScenePreloadDataSO gameplayPreloadDataSO;

    private bool viewed = false;


    //────────────────────────────────────────────────────
    // == Unity Lifecycle ==
    //────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        DataManager.Instance.LoadCachedSaveData();
    }

    private void Start()
    {
        // Start background music
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(0);

        DataManager.Instance.NotifySceneReady();
    }


    //────────────────────────────────────────────────────
    // == Game Start ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Loads the gameplay scene with preload data.
    /// </summary>
    public void StartGame()
    {
        SceneLoadRequest.DataToPreload = gameplayPreloadDataSO;
        SceneManager.LoadScene(1);
    }


    //────────────────────────────────────────────────────
    // == Panel Controls ==
    //────────────────────────────────────────────────────

    /// <summary>Shows or hides the settings panel.</summary>
    public void ToggleSettingsPanel(bool show) => TogglePanel(settingsPanel, show);

    /// <summary>Shows or hides the instruction panel.</summary>
    public void ToggleInstructPanel(bool show) => TogglePanel(instructPanel, show);

    /// <summary>Shows or hides the quit confirmation panel.</summary>
    public void ToggleQuitPanel(bool show) => TogglePanel(quitPanel, show);

    /// <summary>
    /// Enables or disables a given menu panel.
    /// </summary>
    private void TogglePanel(GameObject panel, bool show)
    {
        if (panel == null) return;

        panel.SetActive(show);
        AudioManager.Instance.PlayInteractSound(8);
    }


    //────────────────────────────────────────────────────
    // == Story Flow: Open Story ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Starts async flow to open the story view with fade transition.
    /// </summary>
    public void OpenStorySequence()
    {
        AudioManager.Instance.PlayInteractSound(8);
        StartCoroutine(OpenStoryFlow());
    }

    /// <summary>
    /// Fade-out → reset story → show panel → fade-in.
    /// </summary>
    private IEnumerator OpenStoryFlow()
    {
        fader.gameObject.SetActive(true);
        yield return fader.FadeOutCo(1f);

        StoryManager.Instance.ResetStory();
        storyPanel.SetActive(true);

        yield return fader.FadeInCo(1f);
    }


    //────────────────────────────────────────────────────
    // == Story Flow: End Story ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Starts async flow to close the story view.
    /// </summary>
    public void StartStoryEndSequence()
    {
        StartCoroutine(EndStoryFlow());
    }

    /// <summary>
    /// Fade-out → hide panel → fade-in.
    /// </summary>
    private IEnumerator EndStoryFlow()
    {
        fader.gameObject.SetActive(true);
        yield return fader.FadeOutCo(1f);

        storyPanel.SetActive(false);

        // Save only on first view
        AutoSaveStoryViewed();

        yield return fader.FadeInCo(1f);
    }


    //────────────────────────────────────────────────────
    // == Quit Game ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Exits the application.
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }


    //────────────────────────────────────────────────────
    // == Save & Load ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Saves first-time story view only once.
    /// </summary>
    public void AutoSaveStoryViewed()
    {
        if (viewed) return;

        viewed = true;
        StoryManager.Instance.SetSkipInteractable(viewed);
        SaveManager.Data.hasViewedStoryFirstTime = true;
        SaveManager.SaveGame();
    }

    /// <summary>
    /// Loads story-view state and initializes skip-button + auto-open logic.
    /// </summary>
    public void ImportSaveData(SaveData data)
    {
        if (data == null) return;

        viewed = data.hasViewedStoryFirstTime;

        StoryManager.Instance.SetSkipInteractable(viewed);

        if (!viewed) StartCoroutine(OpenStoryFlow());
    }
}
