using UnityEngine;

public class TestLibreTranslate : MonoBehaviour
{
    // Địa chỉ API: bạn có thể dùng server công khai này trước
    private string apiUrl = "https://translate.astian.org/translate";

    void Start()
    {
        // Dịch thử một câu
        StartCoroutine(TranslatorLibre.Translate("Hello world", "vi", apiUrl, result =>
        {
            Debug.Log("🔤 Translated result: " + result);
        }));
    }
}