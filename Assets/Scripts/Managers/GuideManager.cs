using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages guide tabs and displays the corresponding guide content (images + localized descriptions).
/// </summary>
public class GuideManager : MonoBehaviour
{
    // Define the Localization Table Name constant
    private const string GUIDE_TABLE = "Guide Labels";

    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("Guide Data Settings")]
    [SerializeField] private List<GuideCardDataSO> guideDataList = new();   // All guide entries (one per tab)

    [Header("Tab UI")]
    [SerializeField] private Transform tabContainer;                        // Parent for all tab items (ScrollView content)
    [SerializeField] private GuideTabItem tabItemPrefab;                    // Prefab for a single tab item
    [SerializeField] private ScrollRect tabScrollRect;                      // ScrollRect that scrolls the tabs

    [Header("Tab Navigation Buttons")]
    [SerializeField] private Button prevTabButton;                          // Go to previous tab
    [SerializeField] private Button nextTabButton;                          // Go to next tab

    [Header("Guide Layout Variants")]
    [SerializeField] private GameObject singleImageLayout;                  // Layout used when there is 1 guide entry
    [SerializeField] private GameObject doubleImageLayout;                  // Layout used when there are 2+ guide entries

    [Header("Guide Images")]
    [SerializeField] private Image singleImageSlot;                         // Image for single-image layout
    [SerializeField] private Image[] doubleImageSlots;                      // Images for multi-image layout (size = 2)

    [Header("Guide Descriptions")]
    [SerializeField] private TextMeshProUGUI descriptionText;               // Description for single layout
    [SerializeField] private TextMeshProUGUI doubleDescriptionText01;       // Description for guide item 1
    [SerializeField] private TextMeshProUGUI doubleDescriptionText02;       // Description for guide item 2

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Private Runtime Fields ===

    private readonly List<GuideTabItem> spawnedTabs = new();                // Runtime tabs list
    private int currentTabIndex = -1;                                       // Index of currently selected tab

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

    private void Start()
    {
        SpawnAllTabs();
        WireNavigationButtons();
        AutoSelectFirstTab();

        // Register to update text when language changes
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.RegisterForGlobalRefresh(OnLanguageChanged);
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.UnregisterForGlobalRefresh(OnLanguageChanged);
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Tab Spawn & Clear ===

    /// <summary>
    /// Spawn all tab items from guideDataList.
    /// </summary>
    private void SpawnAllTabs()
    {
        ClearExistingTabs();

        if (tabItemPrefab == null || tabContainer == null)
            return;

        foreach (var data in guideDataList)
        {
            if (data == null) continue;

            var tabItem = Instantiate(tabItemPrefab, tabContainer);
            int capturedIndex = spawnedTabs.Count;
            spawnedTabs.Add(tabItem);

            // Note: GuideTabItem.Configure should handle localizing the tab name using data.systemNameKey
            tabItem.Configure(data, () => SelectTabByIndex(capturedIndex));
        }
    }

    private void ClearExistingTabs()
    {
        foreach (var tab in spawnedTabs)
        {
            if (tab != null) Destroy(tab.gameObject);
        }
        spawnedTabs.Clear();
        currentTabIndex = -1;
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Navigation Buttons Wiring ===

    private void WireNavigationButtons()
    {
        if (prevTabButton != null)
        {
            prevTabButton.onClick.RemoveAllListeners();
            prevTabButton.onClick.AddListener(GoToPreviousTab);
        }

        if (nextTabButton != null)
        {
            nextTabButton.onClick.RemoveAllListeners();
            nextTabButton.onClick.AddListener(GoToNextTab);
        }
        UpdateNavigationButtonsState();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Tab Selection & Navigation ===

    private void AutoSelectFirstTab()
    {
        if (spawnedTabs.Count == 0)
        {
            ClearAllDescriptions();
            HideAllLayouts();
            UpdateNavigationButtonsState();
            return;
        }
        SelectTabByIndex(0);
    }

    private void SelectTabByIndex(int index)
    {
        if (index < 0 || index >= spawnedTabs.Count) return;

        // Deselect previous
        if (currentTabIndex >= 0 && currentTabIndex < spawnedTabs.Count)
        {
            var previousTab = spawnedTabs[currentTabIndex];
            if (previousTab != null) previousTab.SetSelected(false);
        }

        // Select new
        currentTabIndex = index;
        var newTab = spawnedTabs[currentTabIndex];

        if (newTab != null)
        {
            newTab.SetSelected(true);
            DisplayGuideContent(newTab.GuideData);
        }

        UpdateNavigationButtonsState();
    }

    private void GoToPreviousTab()
    {
        if (currentTabIndex <= 0) return;
        SelectTabByIndex(currentTabIndex - 1);
        CenterCurrentTabInScrollView();
        AudioManager.Instance.PlayInteractSound(8);
    }

    private void GoToNextTab()
    {
        if (currentTabIndex < 0 || currentTabIndex >= spawnedTabs.Count - 1) return;
        SelectTabByIndex(currentTabIndex + 1);
        CenterCurrentTabInScrollView();
        AudioManager.Instance.PlayInteractSound(8);
    }

    private void UpdateNavigationButtonsState()
    {
        if (prevTabButton != null)
            prevTabButton.interactable = currentTabIndex > 0;

        if (nextTabButton != null)
            nextTabButton.interactable = currentTabIndex >= 0 && currentTabIndex < spawnedTabs.Count - 1;
    }

    private void CenterCurrentTabInScrollView()
    {
        if (tabScrollRect == null || tabContainer is not RectTransform contentRect) return;
        if (currentTabIndex <= 0 || currentTabIndex >= spawnedTabs.Count - 1) return;

        var currentTab = spawnedTabs[currentTabIndex];
        if (currentTab == null) return;
        var tabRect = currentTab.transform as RectTransform;
        if (tabRect == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        float contentWidth = contentRect.rect.width;
        float viewportWidth = ((RectTransform)tabScrollRect.viewport).rect.width;
        if (contentWidth <= viewportWidth) return;

        Vector3 worldCenter = tabRect.TransformPoint(tabRect.rect.center);
        Vector3 localCenter = contentRect.InverseTransformPoint(worldCenter);

        float contentLeft = contentRect.rect.xMin;
        float tabCenterFromLeft = localCenter.x - contentLeft;
        float targetLeftPos = tabCenterFromLeft - viewportWidth * 0.5f;
        float maxScrollableDistance = contentWidth - viewportWidth;

        float normalized = Mathf.Clamp01(targetLeftPos / maxScrollableDistance);
        tabScrollRect.horizontalNormalizedPosition = normalized;
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Update / Refresh ===

    /// <summary>
    /// Callback from LocalizationManager when language changes.
    /// </summary>
    private void OnLanguageChanged()
    {
        RefreshTabs();

        // Refresh currently displayed content
        if (currentTabIndex >= 0 && currentTabIndex < spawnedTabs.Count)
        {
            var tab = spawnedTabs[currentTabIndex];
            if (tab != null) DisplayGuideContent(tab.GuideData);
        }
    }

    /// <summary>
    /// Refresh all tab items (e.g. update their titles via Configure).
    /// </summary>
    public void RefreshTabs()
    {
        if (spawnedTabs.Count == 0) return;

        for (int i = 0; i < spawnedTabs.Count; i++)
        {
            var tab = spawnedTabs[i];
            if (tab == null) continue;

            var data = tab.GuideData;
            if (data == null) continue;

            int capturedIndex = i;
            // Re-configure to update tab title localization
            tab.Configure(data, () => SelectTabByIndex(capturedIndex));
        }
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Guide Content Display ===

    /// <summary>
    /// Update UI for selected guide tab using Localized Keys.
    /// </summary>
    public void DisplayGuideContent(GuideCardDataSO data)
    {
        if (data == null)
        {
            ClearAllDescriptions();
            HideAllLayouts();
            return;
        }
        UpdateGuideEntries(data);
    }

    private void UpdateGuideEntries(GuideCardDataSO data)
    {
        int count = (data.guideEntries != null) ? data.guideEntries.Length : 0;

        if (count <= 0)
        {
            ClearAllDescriptions();
            HideAllLayouts();
            return;
        }

        if (count == 1) ShowSingleEntry(data);
        else ShowDoubleEntries(data);
    }

    private void HideAllLayouts()
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(false);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(false);
    }

    /// <summary>
    /// Single Layout: Uses 'descriptionKey' from the ScriptableObject itself.
    /// </summary>
    private void ShowSingleEntry(GuideCardDataSO data)
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(true);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(false);

        var entry = (data.guideEntries != null && data.guideEntries.Length > 0) ? data.guideEntries[0] : null;

        // Set Image
        if (singleImageSlot != null)
        {
            singleImageSlot.sprite = entry != null ? entry.image : null;
            singleImageSlot.enabled = (singleImageSlot.sprite != null);
        }

        // Set Text using key
        ApplyDescription(descriptionText, data.descriptionKey);
    }

    /// <summary>
    /// Double Layout: Uses 'entryKey' from each GuideImageEntry.
    /// </summary>
    private void ShowDoubleEntries(GuideCardDataSO data)
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(false);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(true);

        var entries = data.guideEntries;

        // The main description text in Double Layout (optional, using main descriptionKey)
        ApplyDescription(descriptionText, data.descriptionKey);

        // --- Images ---
        for (int i = 0; i < (doubleImageSlots?.Length ?? 0); i++)
        {
            var slot = doubleImageSlots[i];
            if (slot == null) continue;

            bool hasEntry = entries != null && i < entries.Length && entries[i] != null;
            slot.gameObject.SetActive(hasEntry);

            if (hasEntry)
            {
                slot.sprite = entries[i].image;
                slot.enabled = (slot.sprite != null);
            }
        }

        // --- Descriptions (using entryKey) ---
        var e0 = (entries != null && entries.Length > 0) ? entries[0] : null;
        var e1 = (entries != null && entries.Length > 1) ? entries[1] : null;

        ApplyDescription(doubleDescriptionText01, e0 != null ? e0.entryKey : null);
        ApplyDescription(doubleDescriptionText02, e1 != null ? e1.entryKey : null);

        // Rebuild Layout
        var rect = doubleImageLayout != null ? doubleImageLayout.GetComponent<RectTransform>() : null;
        if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    /// <summary>
    /// Uses LocalizationManager to set the text based on the key.
    /// </summary>
    private void ApplyDescription(TextMeshProUGUI label, string key)
    {
        if (label == null) return;

        bool hasKey = !string.IsNullOrWhiteSpace(key);
        label.gameObject.SetActive(hasKey);

        if (hasKey && LocalizationManager.Instance != null)
        {
            // Use the constant table name and the specific key
            LocalizationManager.Instance.SetLocalizedText(label, GUIDE_TABLE, key);
        }
    }

    private void ClearAllDescriptions()
    {
        ApplyDescription(descriptionText, null);
        ApplyDescription(doubleDescriptionText01, null);
        ApplyDescription(doubleDescriptionText02, null);
    }

    #endregion
}