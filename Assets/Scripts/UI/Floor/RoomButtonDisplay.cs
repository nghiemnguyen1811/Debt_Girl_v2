using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class RoomButtonDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button roomButton;
    [SerializeField] private GameObject lockPanel;
    [SerializeField] private TMP_Text requiredLevelText;

    [Header("Room Data")]
    private int requiredLevel;

    private Image buttonImage;

    public Button RoomButton => roomButton;

    private void Awake()
    {
        roomButton = GetComponent<Button>();
        buttonImage = roomButton.GetComponent<Image>();
    }

    /// <summary>
    /// Refresh the UI depending on unlock status.
    /// </summary>
    public void Refresh(bool unlocked)
    {
        if (unlocked)
        {
            // Player can interact with the button
            roomButton.interactable = true;
            SetButtonAlpha(1f);
            if (lockPanel) lockPanel.SetActive(false);
        }

        else
        {
            // Player cannot interact with the button
            roomButton.interactable = false;
            SetButtonAlpha(0f);
            if (lockPanel) lockPanel.SetActive(true);
            if (requiredLevelText) requiredLevelText.text = $"Lv. {requiredLevel}";
        }
    }

    /// <summary>
    /// Setup required level (for initialization).
    /// </summary>
    public void Setup(int reqLevel)
    {
        requiredLevel = reqLevel;
        if (requiredLevelText) requiredLevelText.text = $"Lv. {requiredLevel}";
    }

    /// <summary>
    /// Change only the alpha of the button’s background image.
    /// </summary>
    private void SetButtonAlpha(float alpha)
    {
        if (buttonImage)
        {
            Color c = buttonImage.color;
            c.a = alpha;
            buttonImage.color = c;
        }
    }
}
