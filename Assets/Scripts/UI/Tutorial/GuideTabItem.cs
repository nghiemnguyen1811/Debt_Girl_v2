using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Represents a single guide tab button (title + underline highlight).
/// </summary>
public class GuideTabItem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("Config")]
    [SerializeField] private UIColorsConfig uiColorsConfig;      // Global UI color config (guideTabOn/guideTabOff)

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleLabel;         // Tab title text
    [SerializeField] private Button tabButton;                   // Button to select this tab
    [SerializeField] private Image selectionUnderline;           // Underline shown when selected

    [Header("Data")]
    private GuideCardDataSO guideData;                           // Data for this guide tab

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region === Public Properties ===

    public GuideCardDataSO GuideData => guideData;

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region === Public API ===

    /// <summary>
    /// Configures this card with data and optional click callback (called by GuideManager).
    /// </summary>
    public void Configure(GuideCardDataSO data, UnityAction onClick = null)
    {
        guideData = data;
        RefreshUI();

        if (tabButton == null) return;

        tabButton.onClick.RemoveAllListeners();

        // External callback (GuideManager) handles selection + content update.
        if (onClick != null)
            tabButton.onClick.AddListener(onClick);

        // Newly created tabs start as unselected.
        SetSelected(false);
    }

    /// <summary>
    /// Sets this tab as selected or unselected (color + underline visibility).
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (uiColorsConfig == null)
        {
            Debug.LogWarning($"{nameof(GuideTabItem)} on '{name}' has no UIColorsConfig assigned.");
            return;
        }

        // Use dedicated guide tab colors
        Color activeColor = uiColorsConfig.guideTabOn;
        Color inactiveColor = uiColorsConfig.guideTabOff;

        if (titleLabel != null)
            titleLabel.color = isSelected ? activeColor : inactiveColor;

        if (selectionUnderline != null)
        {
            selectionUnderline.gameObject.SetActive(isSelected);

            if (isSelected)
                selectionUnderline.color = activeColor;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region === Internal Helpers ===

    /// <summary>
    /// Updates UI elements (title text) based on current data.
    /// </summary>
    private void RefreshUI()
    {
        if (guideData == null) return;

        if (titleLabel != null)
            titleLabel.text = guideData.systemName;
    }

    #endregion
}
