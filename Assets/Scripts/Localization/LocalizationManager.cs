using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    [Header("LibreTranslate API URL")]
    [Tooltip("Link API server của bạn hoặc dùng tạm public server")]
    [SerializeField] private string apiUrl = "https://libretranslate.com/translate";

    // Cache tạm (RAM)
    private Dictionary<string, string> translationCache = new Dictionary<string, string>();

    private void Awake()
    {
        // Đảm bảo chỉ có 1 object duy nhất
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Dịch toàn bộ TextMeshProUGUI trong scene hiện tại
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

            // Bỏ qua text trống
            if (string.IsNullOrWhiteSpace(original))
                continue;

            yield return Translate(original, lang, translated =>
            {
                t.text = translated;
            });
        }
    }

    /// <summary>
    /// Dịch 1 đoạn text, có cache để tránh gọi lại
    /// </summary>
    public IEnumerator Translate(string source, string targetLang, System.Action<string> callback)
    {
        string key = $"{source}_{targetLang}";

        // 🔹 Nếu đã có trong cache, trả về luôn
        if (translationCache.TryGetValue(key, out var cached))
        {
            callback(cached);
            yield break;
        }

        // 🔹 Nếu chưa có → gọi API
        yield return TranslatorLibre.Translate(source, targetLang, apiUrl, result =>
        {
            translationCache[key] = result;
            callback(result);
        });
    }
}