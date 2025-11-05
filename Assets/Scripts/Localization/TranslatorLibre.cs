using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// Simple translator using LibreTranslate REST API
/// </summary>
public static class TranslatorLibre
{
    [System.Serializable]
    private class TranslationResult
    {
        public string translatedText;
    }

    /// <summary>
    /// Sends a translation request to LibreTranslate API
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="targetLang">Target language code (e.g. "vi", "en", "ko")</param>
    /// <param name="apiUrl">LibreTranslate API endpoint</param>
    /// <param name="callback">Callback receiving translated text</param>
    public static IEnumerator Translate(string text, string targetLang, string apiUrl, System.Action<string> callback)
    {
        WWWForm form = new WWWForm();
        form.AddField("q", text);
        form.AddField("source", "auto");      // detect automatically
        form.AddField("target", targetLang);

        using (UnityWebRequest request = UnityWebRequest.Post(apiUrl, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                try
                {
                    var result = JsonUtility.FromJson<TranslationResult>(json);
                    callback(result.translatedText);
                }
                catch
                {
                    Debug.LogWarning($"⚠️ Parse error: {json}");
                    callback(text); // fallback to original text
                }
            }
            else
            {
                Debug.LogError($"❌ Translation failed: {request.error}");
                callback(text);
            }
        }
    }
}
