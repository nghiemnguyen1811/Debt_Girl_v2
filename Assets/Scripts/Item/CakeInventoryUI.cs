using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CakeInventoryUI : InventoryBase<CakeInventoryUI>
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private TextMeshProUGUI slotUsageText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private GameObject[] infoDisplayGroup;

    [Header("UI Buttons")]
    [SerializeField] private Button sellButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button closeButton;

    [Header("Stand Setup")]
    [SerializeField] private Transform standContainer;
    [SerializeField] private GameObject standPrefab;
    [SerializeField] private VerticalLayoutGroup layoutGroup;

    #region === Unity Lifecycle ===

    /// <summary>
    /// Initialize UI button listeners.
    /// </summary>
    protected override void Start()
    {
        base.Start();

        sellButton.onClick.AddListener(UI_Sell);
        dropButton.onClick.AddListener(UI_Drop);
        closeButton.onClick.AddListener(UI_Close);
    }

    /// <summary>
    /// Runs after all inventory slots are created.
    /// </summary>
    protected override void OnInventoryInitialized()
    {
        base.OnInventoryInitialized();
        SpawnStandBySlotCount();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Stand Generation Logic ===

    /// <summary>
    /// Creates stands based on slot count (1 stand per 4 slots).
    /// Also adjusts spacing after the first stand.
    /// </summary>
    private void SpawnStandBySlotCount()
    {
        if (standPrefab == null || standContainer == null)
            return;

        int totalSlots = slots.Count;
        int standCount = Mathf.CeilToInt(totalSlots / 4f);

        // Create stands
        for (int i = 0; i < standCount; i++)
            Instantiate(standPrefab, standContainer);

        // Adjust spacing based on number of extra stands
        if (layoutGroup != null)
        {
            int extraStandCount = Mathf.Max(0, standCount - 1);
            float newSpacing = layoutGroup.spacing - 350f * extraStandCount;
            layoutGroup.spacing = newSpacing;
        }
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Item Selection & UI Updates ===

    /// <summary>
    /// Updates UI when an inventory item is selected.
    /// </summary>
    protected override void OnItemSelected(ItemSlot slot, bool playFeedback)
    {
        var data = slot.ItemData;

        itemIconImage.sprite = data.icon;
        itemIconImage.gameObject.SetActive(true);

        LocalizationManager.Instance.SetLocalizedText(itemNameText, "Cake Labels", data.itemNameKey);
        LocalizationManager.Instance.SetLocalizedText(itemDescriptionText, "Cake Labels", data.itemDescriptionKey);

        string formattedValue = DoubleUtilities.ToIdleNotation(data.SellPrice);
        string currency = LocalizationManager.Instance.GetCurrencySymbol();
        sellPriceText.text = $"{formattedValue}{currency}";

        foreach (var infoObj in infoDisplayGroup)
            infoObj.SetActive(true);

        if (playFeedback)
            AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Clears UI when no item is selected.
    /// </summary>
    protected override void OnItemDeselected()
    {
        itemNameText.text = "";
        itemDescriptionText.text = "";

        foreach (var infoObj in infoDisplayGroup)
            infoObj.SetActive(false);
    }

    /// <summary>
    /// Updates (used/total) slot count UI.
    /// </summary>
    protected override void OnSlotUsageChanged(int used, int total)
    {
        slotUsageText.text = $"({used} / {total})";
    }

    /// <summary>
    /// Displays full-inventory warning.
    /// </summary>
    protected override void OnInventoryFull(string message)
    {
        UIManager.Instance.ShowWarningText(message);
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Item Actions ===

    /// <summary>
    /// Handles selling logic for selected item.
    /// </summary>
    protected override void OnSellSelectedItem(ItemDataSO data)
    {
        MoneyManager.Instance.ChangeMoneys(data.SellPrice);
        AudioManager.Instance.PlayInteractSound(1);
    }

    /// <summary>
    /// Feedback only when dropping an item.
    /// </summary>
    protected override void OnDropSelectedItem(ItemDataSO data)
    {
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Called when closing inventory panel.
    /// </summary>
    protected override void OnCloseRequested()
    {
        playerControl.interactDetector.StopCurrentInteraction();
        AudioManager.Instance.PlayInteractSound(8);
    }

    #endregion
}
