using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages guide tabs and displays the corresponding guide content (images + description).
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

    [Header("Tab Navigation Buttons")]
    [SerializeField] private Button prevTabButton;                          // Go to previous tab
    [SerializeField] private Button nextTabButton;                          // Go to next tab

    [Header("Guide Layout Variants")]
    [SerializeField] private GameObject singleImageLayout;                  // Layout used when there is 1 guide image
    [SerializeField] private GameObject doubleImageLayout;                  // Layout used when there are 2+ guide images

    [Header("Guide Images")]
    [SerializeField] private Image singleImageSlot;                         // Image for single-image layout
    [SerializeField] private Image[] doubleImageSlots;                      // Images for multi-image layout (size = 2)

    [Header("Guide Description")]
    [SerializeField] private TextMeshProUGUI descriptionText;               // Text showing guide description

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Private Runtime Fields ===

    private readonly List<GuideTabItem> spawnedTabs = new();                // Runtime tabs list
    private int currentTabIndex = -1;                                       // Index of currently selected tab

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

    /// <summary>
    /// Spawns all tabs, wires nav buttons, and auto-selects the first tab.
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
    /// Spawns all tab items based on guideDataList.
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

            // Capture index AFTER adding to list so it matches runtime ordering
            int capturedIndex = spawnedTabs.Count;
            spawnedTabs.Add(tabItem);

            tabItem.Configure(data, () => SelectTabByIndex(capturedIndex));
        }
    }

    /// <summary>
    /// Destroys all existing tab items and clears the list.
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
    /// Registers click events for Prev/Next navigation buttons.
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
    /// Automatically selects the first tab and shows its content.
    /// </summary>
    private void AutoSelectFirstTab()
    {
        if (spawnedTabs.Count == 0)
        {
            ClearDescription();
            HideAllLayouts();
            UpdateNavigationButtonsState();
            return;
        }

        SelectTabByIndex(0);
    }

    /// <summary>
    /// Selects a tab by index, updates visuals and guide content.
    /// </summary>
    private void SelectTabByIndex(int index)
    {
        if (index < 0 || index >= spawnedTabs.Count)
            return;

        // Unselect previous tab
        if (currentTabIndex >= 0 && currentTabIndex < spawnedTabs.Count)
        {
            var previousTab = spawnedTabs[currentTabIndex];
            if (previousTab != null)
                previousTab.SetSelected(false);
        }

        // Select new tab
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
    /// Moves selection to the previous tab (if possible).
    /// </summary>
    private void GoToPreviousTab()
    {
        if (currentTabIndex <= 0)
            return;

        SelectTabByIndex(currentTabIndex - 1);
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Moves selection to the next tab (if possible).
    /// </summary>
    private void GoToNextTab()
    {
        if (currentTabIndex < 0 || currentTabIndex >= spawnedTabs.Count - 1)
            return;

        SelectTabByIndex(currentTabIndex + 1);
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Updates Prev/Next button interactable state based on current index.
    /// </summary>
    private void UpdateNavigationButtonsState()
    {
        if (prevTabButton != null)
            prevTabButton.interactable = currentTabIndex > 0;

        if (nextTabButton != null)
            nextTabButton.interactable = currentTabIndex >= 0 &&
                                         currentTabIndex < spawnedTabs.Count - 1;
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Update / Refresh ===

    /// <summary>
    /// Refreshes tab labels and callbacks (e.g. after localization changes).
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

        else SelectTabByIndex(currentTabIndex);
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Guide Content Display ===

    /// <summary>
    /// Entry point: updates description text and image layouts for the selected guide.
    /// </summary>
    public void DisplayGuideContent(GuideCardDataSO data)
    {
        if (data == null)
        {
            ClearDescription();
            HideAllLayouts();
            return;
        }

        SetDescription(data);
        UpdateGuideImages(data);
    }

    /// <summary>
    /// Sets the description text based on guide data.
    /// </summary>
    private void SetDescription(GuideCardDataSO data)
    {
        if (descriptionText == null) return;
        descriptionText.text = data.description;
    }

    /// <summary>
    /// Clears the description text when no guide is selected.
    /// </summary>
    private void ClearDescription()
    {
        if (descriptionText == null) return;
        descriptionText.text = string.Empty;
    }

    /// <summary>
    /// Decides which image layout to use based on image count.
    /// </summary>
    private void UpdateGuideImages(GuideCardDataSO data)
    {
        int imageCount = (data.guideImages != null) ? data.guideImages.Length : 0;

        if (imageCount <= 0)
        {
            HideAllLayouts();
            return;
        }

        if (imageCount == 1)
            ShowSingleImageLayout(data.guideImages[0]);

        else ShowDoubleImageLayout(data.guideImages);
    }

    /// <summary>
    /// Hides both single and double image layouts.
    /// </summary>
    private void HideAllLayouts()
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(false);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(false);
    }

    /// <summary>
    /// Shows the single-image layout and assigns its sprite.
    /// </summary>
    private void ShowSingleImageLayout(Sprite sprite)
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(true);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(false);

        if (singleImageSlot == null) return;

        singleImageSlot.sprite = sprite;
        singleImageSlot.enabled = (sprite != null);
    }

    /// <summary>
    /// Shows the double-image layout and assigns up to two sprites.
    /// </summary>
    private void ShowDoubleImageLayout(Sprite[] sprites)
    {
        if (singleImageLayout != null) singleImageLayout.SetActive(false);
        if (doubleImageLayout != null) doubleImageLayout.SetActive(true);

        if (doubleImageSlots == null || doubleImageSlots.Length == 0)
            return;

        for (int i = 0; i < doubleImageSlots.Length; i++)
        {
            var slot = doubleImageSlots[i];
            if (slot == null)
                continue;

            if (sprites != null && i < sprites.Length && sprites[i] != null)
            {
                slot.sprite = sprites[i];
                slot.enabled = true;
            }

            else slot.enabled = false;
        }
    }

    #endregion
}
