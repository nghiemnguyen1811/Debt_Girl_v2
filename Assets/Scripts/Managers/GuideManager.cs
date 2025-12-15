using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages guide tabs and displays the corresponding guide content (images + per-item descriptions).
/// </summary>
public class GuideManager : MonoBehaviour
{
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
    [SerializeField] private TextMeshProUGUI descriptionText;          // Description for single layout
    [SerializeField] private TextMeshProUGUI doubleDescriptionText01;        // Description for guide item 1
    [SerializeField] private TextMeshProUGUI doubleDescriptionText02;        // Description for guide item 2

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Private Runtime Fields ===

    private readonly List<GuideTabItem> spawnedTabs = new();                // Runtime tabs list
    private int currentTabIndex = -1;                                       // Index of currently selected tab

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

    /// <summary>
    /// Spawn tabs, wire buttons, auto-select first tab.
    /// </summary>
    private void Start()
    {
        SpawnAllTabs();
        WireNavigationButtons();
        AutoSelectFirstTab();
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
            if (data == null)
                continue;

            var tabItem = Instantiate(tabItemPrefab, tabContainer);

            int capturedIndex = spawnedTabs.Count;
            spawnedTabs.Add(tabItem);

            tabItem.Configure(data, () => SelectTabByIndex(capturedIndex));
        }
    }

    /// <summary>
    /// Destroy old tabs and reset selection.
    /// </summary>
    private void ClearExistingTabs()
    {
        foreach (var tab in spawnedTabs)
        {
            if (tab != null)
                Destroy(tab.gameObject);
        }

        spawnedTabs.Clear();
        currentTabIndex = -1;
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Navigation Buttons Wiring ===

    /// <summary>
    /// Wire Prev/Next buttons.
    /// </summary>
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

    /// <summary>
    /// Auto select first tab (or clear UI if empty).
    /// </summary>
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

    /// <summary>
    /// Select tab by index and display its content.
    /// </summary>
    private void SelectTabByIndex(int index)
    {
        if (index < 0 || index >= spawnedTabs.Count)
            return;

        if (currentTabIndex >= 0 && currentTabIndex < spawnedTabs.Count)
        {
            var previousTab = spawnedTabs[currentTabIndex];
            if (previousTab != null)
                previousTab.SetSelected(false);
        }

        currentTabIndex = index;
        var newTab = spawnedTabs[currentTabIndex];

        if (newTab != null)
        {
            newTab.SetSelected(true);
            DisplayGuideContent(newTab.GuideData);
        }

        UpdateNavigationButtonsState();
    }

    /// <summary>
    /// Go to previous tab.
    /// </summary>
    private void GoToPreviousTab()
    {
        if (currentTabIndex <= 0)
            return;

        SelectTabByIndex(currentTabIndex - 1);
        CenterCurrentTabInScrollView();
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Go to next tab.
    /// </summary>
    private void GoToNextTab()
    {
        if (currentTabIndex < 0 || currentTabIndex >= spawnedTabs.Count - 1)
            return;

        SelectTabByIndex(currentTabIndex + 1);
        CenterCurrentTabInScrollView();
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Enable/disable Prev/Next based on current index.
    /// </summary>
    private void UpdateNavigationButtonsState()
    {
        if (prevTabButton != null)
            prevTabButton.interactable = currentTabIndex > 0;

        if (nextTabButton != null)
            nextTabButton.interactable = currentTabIndex >= 0 &&
                                         currentTabIndex < spawnedTabs.Count - 1;
    }

    /// <summary>
    /// Center current tab in ScrollView (except first/last).
    /// </summary>
    private void CenterCurrentTabInScrollView()
    {
        if (tabScrollRect == null)
            return;

        if (currentTabIndex <= 0 || currentTabIndex >= spawnedTabs.Count - 1)
            return;

        if (tabContainer is not RectTransform contentRect)
            return;

        var currentTab = spawnedTabs[currentTabIndex];
        if (currentTab == null)
            return;

        var tabRect = currentTab.transform as RectTransform;
        if (tabRect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        float contentWidth = contentRect.rect.width;
        float viewportWidth = ((RectTransform)tabScrollRect.viewport).rect.width;

        if (contentWidth <= viewportWidth)
            return;

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
    /// Refresh all tabs (e.g. localization).
    /// </summary>
    public void RefreshTabs()
    {
        if (spawnedTabs.Count == 0)
            return;

        for (int i = 0; i < spawnedTabs.Count; i++)
        {
            var tab = spawnedTabs[i];
            if (tab == null)
                continue;

            var data = tab.GuideData;
            if (data == null)
                continue;

            int capturedIndex = i;
            tab.Configure(data, () => SelectTabByIndex(capturedIndex));
        }

        if (currentTabIndex < 0 || currentTabIndex >= spawnedTabs.Count)
            AutoSelectFirstTab();
        else
        {
            SelectTabByIndex(currentTabIndex);
            CenterCurrentTabInScrollView();
        }
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Guide Content Display ===

    /// <summary>
    /// Update UI for selected guide tab (images + per-item descriptions).
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

    /// <summary>
    /// Decide single/double layout based on guideEntries count.
    /// </summary>
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

    /// <summary>
    /// Hide both layouts.
    /// </summary>
    private void HideAllLayouts()
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(false);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(false);
    }

    /// <summary>
    /// Show single layout and apply image + description.
    /// </summary>
    private void ShowSingleEntry(GuideCardDataSO data)
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(true);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(false);

        var entry = (data != null && data.guideEntries != null && data.guideEntries.Length > 0)
            ? data.guideEntries[0]
            : null;

        if (singleImageSlot != null)
        {
            singleImageSlot.sprite = entry != null ? entry.image : null;
            singleImageSlot.enabled = (singleImageSlot.sprite != null);
        }

        // ✅ dùng description chung của GuideCardDataSO
        ApplyDescription(descriptionText, data != null ? data.description : null);
    }

    /// <summary>
    /// Show double layout and apply up to 2 entries (image + description per item).
    /// </summary>
    private void ShowDoubleEntries(GuideCardDataSO data)
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(false);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(true);

        var entries = data != null ? data.guideEntries : null;

        // ✅ DOUBLE cũng hiện descriptionText (mô tả chung)
        ApplyDescription(descriptionText, data != null ? data.description : null);

        // Images (max 2 slots)
        for (int i = 0; i < (doubleImageSlots?.Length ?? 0); i++)
        {
            var slot = doubleImageSlots[i];
            if (slot == null) continue;

            bool hasEntry = entries != null && i < entries.Length && entries[i] != null && entries[i].image != null;
            slot.gameObject.SetActive(hasEntry);

            if (hasEntry)
            {
                slot.sprite = entries[i].image;
                slot.enabled = true;
            }
            else
            {
                slot.sprite = null;
                slot.enabled = false;
            }
        }

        // Descriptions for 2 guide items (auto hide if null/empty)
        var e0 = (entries != null && entries.Length > 0) ? entries[0] : null;
        var e1 = (entries != null && entries.Length > 1) ? entries[1] : null;

        ApplyDescription(doubleDescriptionText01, e0 != null ? e0.description : null);
        ApplyDescription(doubleDescriptionText02, e1 != null ? e1.description : null);

        // (optional)
        var rect = doubleImageLayout != null ? doubleImageLayout.GetComponent<RectTransform>() : null;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }


    /// <summary>
    /// Set TMP text and toggle active based on null/empty.
    /// </summary>
    private void ApplyDescription(TextMeshProUGUI label, string text)
    {
        if (label == null) return;

        bool hasText = !string.IsNullOrWhiteSpace(text);
        label.gameObject.SetActive(hasText);

        if (hasText)
            label.text = text;
    }

    /// <summary>
    /// Clear & hide all description labels.
    /// </summary>
    private void ClearAllDescriptions()
    {
        ApplyDescription(descriptionText, null);
        ApplyDescription(doubleDescriptionText01, null);
        ApplyDescription(doubleDescriptionText02, null);
    }

    #endregion
}
