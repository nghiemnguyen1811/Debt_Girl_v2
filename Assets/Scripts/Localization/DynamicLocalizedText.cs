using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DynamicLocalizedText : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private string lastText;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();

        LocalizationSettings.SelectedLocaleChanged += _ => TryLocalize();
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= _ => TryLocalize();
    }

    private void Update()
    {
        if (textMesh.text != lastText)
        {
            lastText = textMesh.text;
            TryLocalize();
        }
    }

    private void TryLocalize()
    {
        if (string.IsNullOrEmpty(textMesh.text)) return;

        var localized = LocalizationSettings.StringDatabase.GetLocalizedString("UI_Floors", textMesh.text);
        textMesh.text = string.IsNullOrEmpty(localized) ? textMesh.text : localized;

        textMesh.text = localized;
    }
}
