using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
///     Centralized Localization Manager for the entire game.
///
/// Handles:
/// - Caching of localized strings for better performance.
/// - Broadcasting global refresh events when the language changes.
/// - Providing a single globally cached currency symbol (₩ / $ / ₫ ...).
/// - Simplified API for setting localized text in TextMeshPro labels.
/// </summary>
public class LocalizationManager : SingletonMonobehaviour<LocalizationManager>
{
    //─────────────────────────────────────────────
    #region === Events ===

    /// <summary>
    /// Triggered when the selected language (locale) changes.
    /// Other systems can subscribe to react dynamically.
    /// </summary>
    public event Action OnLanguageChanged;

    #endregion

    //─────────────────────────────────────────────
    #region === Internal Cache ===

    /// <summary>
    /// Cached localized text data to reduce repeated lookups.
    /// Key format: "Table_Key"
    /// </summary>
    private readonly Dictionary<string, string> cachedTexts = new();

    #endregion

    //─────────────────────────────────────────────
    #region === Global Refresh Listeners ===

    /// <summary>
    /// List of global callbacks triggered when the locale changes.
    /// Useful for refreshing inactive UIs or runtime text components.
    /// </summary>
    private readonly List<Action> globalRefreshListeners = new();

    /// <summary>
    /// Registers a listener to be called when the language changes.
    /// </summary>
    public void RegisterForGlobalRefresh(Action listener)
    {
        if (!globalRefreshListeners.Contains(listener))
            globalRefreshListeners.Add(listener);
    }

    /// <summary>
    /// Unregisters a previously registered listener.
    /// </summary>
    public void UnregisterForGlobalRefresh(Action listener)
    {
        globalRefreshListeners.Remove(listener);
    }

    #endregion

    //─────────────────────────────────────────────
    #region === Currency Symbol Cache ===

    /// <summary>
    /// The cached symbol used for all money-related UI (₩ / $ / ₫ ...).
    /// Automatically updates when the locale changes.
    /// </summary>
    private string cachedCurrencySymbol;

    /// <summary>
    /// Reference to the localized entry for currency symbol.
    /// Table: "Shop Labels" | Key: "shop.currencySymbol"
    /// </summary>
    private readonly LocalizedString currencySymbolString =
        new LocalizedString("Shop Labels", "shop.currencySymbol");

    /// <summary>
    /// Retrieves and caches the localized currency symbol.
    /// </summary>
    private void RefreshCurrencySymbol()
    {
        try
        {
            cachedCurrencySymbol = currencySymbolString.GetLocalizedString();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LocalizationManager] Failed to refresh currency symbol: {ex.Message}");
            cachedCurrencySymbol = "$"; // Fallback default
        }
    }

    /// <summary>
    /// Returns the current currency symbol.
    /// Automatically refreshes if cache is empty.
    /// </summary>
    public string GetCurrencySymbol()
    {
        if (string.IsNullOrEmpty(cachedCurrencySymbol))
            RefreshCurrencySymbol();

        return cachedCurrencySymbol;
    }

    #endregion

    //─────────────────────────────────────────────
    #region === Unity Lifecycle ===

    private void OnEnable()
    {
        // Subscribe to locale change events
        LocalizationSettings.SelectedLocaleChanged += HandleLanguageChanged;

        // Subscribe to updates on the currency symbol
        currencySymbolString.StringChanged += OnCurrencySymbolChanged;

        // Initialize symbol cache
        RefreshCurrencySymbol();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLanguageChanged;
        currencySymbolString.StringChanged -= OnCurrencySymbolChanged;
    }

    /// <summary>
    /// Handles language switching:
    /// - Clears cached localized strings.
    /// - Refreshes the currency symbol.
    /// - Invokes refresh listeners for all UI components.
    /// </summary>
    private void HandleLanguageChanged(Locale locale)
    {
        cachedTexts.Clear();
        RefreshCurrencySymbol();

        OnLanguageChanged?.Invoke();

        foreach (var callback in globalRefreshListeners)
        {
            try { callback?.Invoke(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalizationManager] Refresh callback failed: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Updates the cached currency symbol whenever its localized value changes.
    /// </summary>
    private void OnCurrencySymbolChanged(string newSymbol)
    {
        cachedCurrencySymbol = newSymbol;
    }

    #endregion

    //─────────────────────────────────────────────
    #region === Public API ===

    /// <summary>
    /// Sets the localized text for a TMP_Text component based on a given table and key.
    /// Automatically caches the result to avoid redundant lookups.
    /// </summary>
    /// <param name="label">Target TMP_Text to apply localization to.</param>
    /// <param name="table">Name of the localization table.</param>
    /// <param name="key">Key of the localized entry.</param>
    public async void SetLocalizedText(TMP_Text label, string table, string key)
    {
        if (!label || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(table))
            return;

        string cacheKey = $"{table}_{key}";

        // Use cache if available
        if (cachedTexts.TryGetValue(cacheKey, out var cached))
        {
            label.text = cached;
            return;
        }

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
    /// Retrieves a localized string directly for logic or formatting purposes
    /// without updating a UI element.
    /// </summary>
    /// <param name="table">Name of the localization table.</param>
    /// <param name="key">Key of the localized entry.</param>
    /// <returns>Localized string, or an empty string if not found.</returns>
    public async Task<string> GetLocalizedString(string table, string key)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(table))
            return string.Empty;

        string cacheKey = $"{table}_{key}";

        if (cachedTexts.TryGetValue(cacheKey, out var cached))
            return cached;

        var locString = new LocalizedString(table, key);
        try
        {
            string result = await locString.GetLocalizedStringAsync().Task;
            cachedTexts[cacheKey] = result;
            return result;
        }
        catch
        {
            return string.Empty;
        }
    }

    #endregion
}
