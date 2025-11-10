using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class LocalizationManager : SingletonMonobehaviour<LocalizationManager>
{
    [Header("LibreTranslate API URL")]
    [Tooltip("Your API server URL, or use a temporary public server")]
    [SerializeField] private string apiUrl = "https://libretranslate.com/translate";

    // Temporary cache (stored in RAM)
    private Dictionary<string, string> translationCache = new Dictionary<string, string>();

    /// <summary>
    /// Translates all TextMeshProUGUI elements in the current scene.
    /// </summary>
    public void TranslateSceneUI(string targetLang)
    {
        StartCoroutine(TranslateAllTexts(targetLang));
    }

    private IEnumerator TranslateAllTexts(string lang)
    {
        TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

        foreach (var t in texts)
        {
            string original = t.text;

            // Skip empty text
            if (string.IsNullOrWhiteSpace(original))
                continue;

            yield return Translate(original, lang, translated =>
            {
                t.text = translated;
            });
        }
    }

    /// <summary>
    /// Translates a single text string with caching to avoid duplicate API calls.
    /// </summary>
    public IEnumerator Translate(string source, string targetLang, System.Action<string> callback)
    {
        string key = $"{source}_{targetLang}";

        // 🔹 If already cached, return immediately
        if (translationCache.TryGetValue(key, out var cached))
        {
            callback(cached);
            yield break;
        }

        // 🔹 If not cached → call API
        yield return TranslatorLibre.Translate(source, targetLang, apiUrl, result =>
        {
            translationCache[key] = result;
            callback(result);
        });
    }
}