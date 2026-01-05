using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizationManager : SingletonMonobehaviour<LocalizationManager>
{
    public event Action OnLanguageChanged;

    private readonly Dictionary<string, string> cachedTexts = new();
    private readonly List<Action> globalRefreshListeners = new();

    private string cachedCurrencySymbol = "$";

    private readonly LocalizedString currencySymbolString =
        new LocalizedString("Shop Labels", "shop.currencySymbol");

    private Task _initTask;

    protected override void Awake()
    {
        base.Awake();
        _initTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LocalizationSettings.InitializationOperation.Task;

        LocalizationSettings.SelectedLocaleChanged += HandleLanguageChanged;
        currencySymbolString.StringChanged += OnCurrencySymbolChanged;

        await RefreshCurrencySymbolAsync();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLanguageChanged;
        currencySymbolString.StringChanged -= OnCurrencySymbolChanged;
    }

    public void RegisterForGlobalRefresh(Action listener)
    {
        if (listener == null) return;
        if (!globalRefreshListeners.Contains(listener))
            globalRefreshListeners.Add(listener);
    }

    public void UnregisterForGlobalRefresh(Action listener)
    {
        if (listener == null) return;
        globalRefreshListeners.Remove(listener);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initTask != null) await _initTask;
        else await LocalizationSettings.InitializationOperation.Task;
    }

    private async Task RefreshCurrencySymbolAsync()
    {
        try
        {
            // IMPORTANT: do not read op.Result; take value directly from Task
            string symbol = await currencySymbolString.GetLocalizedStringAsync().Task;
            cachedCurrencySymbol = string.IsNullOrEmpty(symbol) ? "$" : symbol;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LocalizationManager] Failed to refresh currency symbol: {ex.Message}");
            cachedCurrencySymbol = "$";
        }
    }

    public string GetCurrencySymbol()
    {
        return string.IsNullOrEmpty(cachedCurrencySymbol) ? "$" : cachedCurrencySymbol;
    }

    private async void HandleLanguageChanged(Locale locale)
    {
        cachedTexts.Clear();

        await RefreshCurrencySymbolAsync();

        OnLanguageChanged?.Invoke();

        for (int i = 0; i < globalRefreshListeners.Count; i++)
        {
            var callback = globalRefreshListeners[i];
            try { callback?.Invoke(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalizationManager] Refresh callback failed: {e.Message}");
            }
        }
    }

    private void OnCurrencySymbolChanged(string newSymbol)
    {
        cachedCurrencySymbol = string.IsNullOrEmpty(newSymbol) ? "$" : newSymbol;
    }

    public async void SetLocalizedText(TMP_Text label, string table, string key)
    {
        if (!label || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(table))
            return;

        await EnsureInitializedAsync();

        // Optional: 1-frame delay to stabilize TMP on device
        await Task.Yield();

        if (!label) return;

        string cacheKey = $"{table}_{key}";

        if (cachedTexts.TryGetValue(cacheKey, out var cached))
        {
            label.text = cached;
            label.ForceMeshUpdate(true, true);
            return;
        }

        try
        {
            var locString = new LocalizedString(table, key);

            // IMPORTANT: do not read op.Result; take value directly from Task
            string result = await locString.GetLocalizedStringAsync().Task;
            result ??= string.Empty;

            cachedTexts[cacheKey] = result;

            if (!label) return;

            label.text = result;
            label.ForceMeshUpdate(true, true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalizationManager] Failed to load key '{key}' from '{table}': {ex.Message}");
        }
    }

    public async Task<string> GetLocalizedString(string table, string key)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(table))
            return string.Empty;

        await EnsureInitializedAsync();

        string cacheKey = $"{table}_{key}";

        if (cachedTexts.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var locString = new LocalizedString(table, key);

            // IMPORTANT: do not read op.Result; take value directly from Task
            string result = await locString.GetLocalizedStringAsync().Task;
            result ??= string.Empty;

            cachedTexts[cacheKey] = result;
            return result;
        }
        catch
        {
            return string.Empty;
        }
    }
}
