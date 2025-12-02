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
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject quitPanel;

    [Header("Story UI")]
    [SerializeField] private GameObject storyPanel;
    [SerializeField] private Fader fader;

    [Header("Preload Data")]
    [SerializeField] private ScenePreloadDataSO gameplayPreloadDataSO;

    //────────────────────────────────────────────────────
    // == Private Fields ==
    //────────────────────────────────────────────────────

    // Tracks whether the intro story has been viewed at least once.
    private bool hasViewedIntroStory;

    //────────────────────────────────────────────────────
    // == Unity Lifecycle ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Initializes singletons and loads cached save data.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        DataManager.Instance.LoadCachedSaveData();
    }

    /// <summary>
    /// Starts BGM, hides menu panels, and notifies DataManager that the scene is ready.
    /// </summary>
    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(0);

        InitializeMenuPanels();
        DataManager.Instance.NotifySceneReady();
    }

    //────────────────────────────────────────────────────
    // == Menu Initialization ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Hides all popup menu panels (settings / tutorial / quit) on startup.
    /// </summary>
    private void InitializeMenuPanels()
    {
        SetPanelActive(settingsPanel, false);
        SetPanelActive(tutorialPanel, false);
        SetPanelActive(quitPanel, false);
    }

    /// <summary>
    /// Safely sets a panel active or inactive without playing sounds.
    /// </summary>
    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel == null) return;
        panel.SetActive(isActive);
    }

    //────────────────────────────────────────────────────
    // == Game Start ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Loads the gameplay scene and passes preload data.
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

    /// <summary>Shows or hides the tutorial panel.</summary>
    public void ToggleTutorialPanel(bool show) => TogglePanel(tutorialPanel, show);

    /// <summary>Shows or hides the quit confirmation panel.</summary>
    public void ToggleQuitPanel(bool show) => TogglePanel(quitPanel, show);

    /// <summary>
    /// Enables or disables a given menu panel and plays a UI click sound.
    /// </summary>
    private void TogglePanel(GameObject panel, bool show)
    {
        if (panel == null) return;

        panel.SetActive(show);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayInteractSound(8);
    }

    //────────────────────────────────────────────────────
    // == Story Flow: Open Story ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Starts async flow to open the story view with a fade transition.
    /// </summary>
    public void OpenStorySequence()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayInteractSound(8);

        StartCoroutine(OpenStoryFlow());
    }

    /// <summary>
    /// Fade-out → reset story → show story panel → fade-in.
    /// </summary>
    private IEnumerator OpenStoryFlow()
    {
        if (fader != null)
        {
            fader.gameObject.SetActive(true);
            yield return fader.FadeOutCo(1f);
        }

        if (StoryManager.Instance != null)
            StoryManager.Instance.ResetStory();

        if (storyPanel != null)
            storyPanel.SetActive(true);

        if (fader != null)
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
    /// Fade-out → hide story panel → auto-save viewed flag → fade-in.
    /// </summary>
    private IEnumerator EndStoryFlow()
    {
        if (fader != null)
        {
            fader.gameObject.SetActive(true);
            yield return fader.FadeOutCo(1f);
        }

        if (storyPanel != null)
            storyPanel.SetActive(false);

        AutoSaveStoryViewed();

        if (fader != null)
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
    /// Saves the "intro story viewed first time" flag only once.
    /// </summary>
    public void AutoSaveStoryViewed()
    {
        if (hasViewedIntroStory) return;

        hasViewedIntroStory = true;

        if (StoryManager.Instance != null)
            StoryManager.Instance.SetSkipInteractable(hasViewedIntroStory);

        SaveManager.Data.hasViewedStoryFirstTime = true;
        SaveManager.SaveGame();
    }

    /// <summary>
    /// Restores story-view state and auto-opens the story if needed.
    /// </summary>
    public void ImportSaveData(SaveData data)
    {
        if (data == null) return;

        hasViewedIntroStory = data.hasViewedStoryFirstTime;

        if (StoryManager.Instance != null)
            StoryManager.Instance.SetSkipInteractable(hasViewedIntroStory);

        // Auto-open story only if it has never been viewed.
        if (!hasViewedIntroStory)
            StartCoroutine(OpenStoryFlow());
    }
}
