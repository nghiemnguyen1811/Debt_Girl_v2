using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GuideCardContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image systemImage;
    [SerializeField] private TextMeshProUGUI systemNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    /// <summary>Assign data from GuideCardDataSO to UI elements.</summary>
    public void Configure(GuideCardDataSO data)
    {
        if (data == null) return;

        if (systemImage) systemImage.sprite = data.icon;
        if (systemNameText) systemNameText.text = data.systemName;
        if (descriptionText) descriptionText.text = data.description;
    }
}
