using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

/// <summary>
/// Centralized localization manager that caches and provides translated strings by key.
/// Supports dynamic runtime usage (e.g., items, floors, shop UI).
/// </summary>
public class LocalizationManager : SingletonMonobehaviour<LocalizationManager>
{
    //─────────────────────────────────────────────
    #region === Events ===

    /// <summary>
    /// Invoked when the active locale (language) changes.
    /// </summary>
    public event Action OnLanguageChanged;

    #endregion

    //─────────────────────────────────────────────
    #region === Internal Cache ===

    /// <summary>
    /// Cache of localized texts by combined key (table_key).
    /// Prevents redundant async loads for the same string.
    /// </summary>
    private readonly Dictionary<string, string> cachedTexts = new();

    #endregion

    //─────────────────────────────────────────────
    #region === Unity Lifecycle ===

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLanguageChanged;
    }

    /// <summary>
    /// Clears cache when language changes and notifies subscribers.
    /// </summary>
    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        cachedTexts.Clear();
        OnLanguageChanged?.Invoke();
    }

    #endregion

    //─────────────────────────────────────────────
    #region === Public API ===

    /// <summary>
    /// Sets the localized text of a TMP_Text component using a table name and key.
    /// Automatically caches results for faster subsequent lookups.
    /// </summary>
    /// <param name="label">The TextMeshProUGUI component to update.</param>
    /// <param name="table">Localization table name.</param>
    /// <param name="key">Entry key inside the table.</param>
    public async void SetLocalizedText(TMP_Text label, string table, string key)
    {
        if (!label || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(table))
        {
            Debug.LogWarning($"[LocalizationManager] Invalid table/key: {table}/{key}");
            return;
        }

        string cacheKey = $"{table}_{key}";

        // Check cache first
        if (cachedTexts.TryGetValue(cacheKey, out var cached))
        {
            label.text = cached;
            return;
        }

        // Load from localization system
        var locString = new LocalizedString(table, key);

        try
        {
            string result = await locString.GetLocalizedStringAsync().Task;
            cachedTexts[cacheKey] = result;
            label.text = result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalizationManager] Failed to load key '{key}' from '{table}': {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves a localized string (without directly updating UI).
    /// Use this when you need the text for logic or formatted composition.
    /// </summary>
    /// <param name="table">Localization table name.</param>
    /// <param name="key">Entry key inside the table.</param>
    /// <returns>Localized text as string.</returns>
    public async System.Threading.Tasks.Task<string> GetLocalizedString(string table, string key)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(table))
            return string.Empty;

        string cacheKey = $"{table}_{key}";

        // Return cached if exists
        if (cachedTexts.TryGetValue(cacheKey, out var cached))
            return cached;

        var locString = new LocalizedString(table, key);

        try
        {
            string result = await locString.GetLocalizedStringAsync().Task;
            cachedTexts[cacheKey] = result;
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalizationManager] Failed to get key '{key}' from '{table}': {ex.Message}");
            return string.Empty;
        }
    }

    #endregion
}
