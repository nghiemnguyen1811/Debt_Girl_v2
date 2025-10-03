using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class RoomButtonDisplay : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private Button roomButton;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image mapMarkerImg;
    [SerializeField] private GameObject lockPanel;
    [SerializeField] private GameObject unlockPanel;
    [SerializeField] private TMP_Text requiredLevelText;

    // ─────────────────────────────────────────────────────
    // Room Data
    // ─────────────────────────────────────────────────────
    private RoomData roomData;

    // ─────────────────────────────────────────────────────
    // Properties
    // ─────────────────────────────────────────────────────
    public RoomType RoomType => roomData != null ? roomData.roomType : RoomType.None;
    public Button RoomButton => roomButton;

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Refresh the UI depending on unlock status.
    /// </summary>
    public void Refresh(bool unlocked)
    {
        if (unlocked)
        {
            SetButtonAlpha(1f);

            if (roomButton) roomButton.interactable = true;
            if (lockPanel) lockPanel.SetActive(false);
        }

        else
        {
            SetButtonAlpha(0f);

            if (roomButton) roomButton.interactable = false;
            if (lockPanel) lockPanel.SetActive(true);
            if (requiredLevelText) requiredLevelText.text = $"Lv. {roomData.level}";
        }
    }

    /// <summary>
    /// Setup room data and initialize UI.
    /// </summary>
    public void Setup(RoomData data)
    {
        roomData = data;

        if (mapMarkerImg) mapMarkerImg.sprite = roomData.markerIcon;
        if (requiredLevelText) requiredLevelText.text = $"Lv. {roomData.level}";
    }

    /// <summary>
    /// Show or hide the unlock panel.
    /// </summary>
    public void ShowUnlockPanel(bool show)
    {
        if (unlockPanel) unlockPanel.SetActive(show);
    }

    // ─────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────

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
