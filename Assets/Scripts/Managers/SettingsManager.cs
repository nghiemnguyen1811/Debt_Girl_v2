using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Handles audio volume sliders and language switching for the game settings menu.
/// The language buttons cycle through available locales in Unity Localization.
/// </summary>
public class SettingsManager : SingletonMonobehaviour<SettingsManager>
{
    [Header("Volume Sliders")]
    public Slider volumeVolSlider;
    public Slider musicVolSlider;
    public Slider soundVolSlider;

    [Header("Language Controls")]
    [SerializeField] private Button leftLangButton;
    [SerializeField] private Button rightLangButton;

    private List<Locale> availableLocales;
    private int currentLocaleIndex = 0;

    // ------------------------------------------------------------
    // Unity Lifecycle
    // ------------------------------------------------------------
    private void Start()
    {
        Initialize();
        SetupLanguageButtons();
    }

    /// <summary>
    /// Initializes all volume sliders with current values from AudioManager.
    /// </summary>
    private void Initialize()
    {
        volumeVolSlider.value = AudioManager.Instance.GetVolumeVol();
        musicVolSlider.value = AudioManager.Instance.GetMusicVol();
        soundVolSlider.value = AudioManager.Instance.GetSoundVol();
    }

    // ------------------------------------------------------------
    // Volume Control
    // ------------------------------------------------------------
    /// <summary>
    /// Updates the master volume using the current slider value.
    /// </summary>
    public void SetVolumeSlider() => AudioManager.Instance.SetVolumeSlider();

    /// <summary>
    /// Updates the music volume using the current slider value.
    /// </summary>
    public void SetMusicSlider() => AudioManager.Instance.SetMusicSlider();

    /// <summary>
    /// Updates the sound effect volume using the current slider value.
    /// </summary>
    public void SetSoundSlider() => AudioManager.Instance.SetSoundSlider();

    // ------------------------------------------------------------
    // Language Switching
    // ------------------------------------------------------------
    /// <summary>
    /// Initializes the language system and binds arrow buttons to change locales.
    /// </summary>
    private async void SetupLanguageButtons()
    {
        // Wait for Unity Localization to finish loading
        await LocalizationSettings.InitializationOperation.Task;

        // Retrieve all available locales from the project settings
        availableLocales = LocalizationSettings.AvailableLocales.Locales;

        // Find the currently selected locale
        var currentLocale = LocalizationSettings.SelectedLocale;
        currentLocaleIndex = availableLocales.IndexOf(currentLocale);

        // Register button listeners
        leftLangButton.onClick.AddListener(SelectPreviousLanguage);
        rightLangButton.onClick.AddListener(SelectNextLanguage);
    }

    /// <summary>
    /// Selects the previous language in the list. Loops around when reaching the start.
    /// </summary>
    private void SelectPreviousLanguage()
    {
        if (availableLocales == null || availableLocales.Count == 0) return;

        currentLocaleIndex = (currentLocaleIndex - 1 + availableLocales.Count) % availableLocales.Count;
        ApplyLanguageChange();
    }

    /// <summary>
    /// Selects the next language in the list. Loops around when reaching the end.
    /// </summary>
    private void SelectNextLanguage()
    {
        if (availableLocales == null || availableLocales.Count == 0) return;

        currentLocaleIndex = (currentLocaleIndex + 1) % availableLocales.Count;
        ApplyLanguageChange();
    }

    /// <summary>
    /// Applies the selected language and triggers Unity Localization to refresh all localized text.
    /// </summary>
    private void ApplyLanguageChange()
    {
        LocalizationSettings.SelectedLocale = availableLocales[currentLocaleIndex];
        Debug.Log($"[SettingsManager] Language changed to: {availableLocales[currentLocaleIndex].LocaleName}");
    }
}
