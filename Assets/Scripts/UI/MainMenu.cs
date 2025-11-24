using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls main menu UI: settings panel, instructions, quitting,
/// story transitions using Fader, and gameplay scene start.
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


    //────────────────────────────────────────────────────
    // == Unity Lifecycle ==
    //────────────────────────────────────────────────────

    private void Start()
    {
        // Play main menu music
        AudioManager.Instance.PlayMusic(0);
    }


    //────────────────────────────────────────────────────
    // == Game Start ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Loads the main gameplay scene using SceneLoadRequest.
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
    /// Handles enabling/disabling any provided panel.
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
    /// Begins the transition into story mode using Fader.
    /// </summary>
    public void OpenStorySequence()
    {
        StartCoroutine(OpenStoryFlow());
    }

    /// <summary>
    /// Fade to black → reset story → show story panel → fade in.
    /// </summary>
    private IEnumerator OpenStoryFlow()
    {
        // Enable fader
        fader.gameObject.SetActive(true);

        // Fade screen to black
        yield return fader.FadeOutCo(1f);

        // Reset all story state
        StoryManager.Instance.ResetStory();

        // Activate story UI
        storyPanel.SetActive(true);

        // Fade screen back to visible
        yield return fader.FadeInCo(1f);
    }


    //────────────────────────────────────────────────────
    // == Story Flow: End Story ==
    //────────────────────────────────────────────────────

    /// <summary>
    /// Starts ending sequence after story finishes.
    /// </summary>
    public void StartStoryEndSequence()
    {
        StartCoroutine(EndStoryFlow());
    }

    /// <summary>
    /// Fade to black → hide storyPanel → fade back in.
    /// </summary>
    private IEnumerator EndStoryFlow()
    {
        fader.gameObject.SetActive(true);

        // Fade screen to black
        yield return fader.FadeOutCo(1f);

        // Hide story view
        storyPanel.SetActive(false);

        // Fade screen back to normal
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
}
