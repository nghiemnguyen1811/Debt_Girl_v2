using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodInventoryUI : InventoryBase<FoodInventoryUI>
{
    [Header("UI Elements")]
    [SerializeField] private Transform itemInfoContainer;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemQuantityText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI moodText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Transform statGroupRoot;
    [SerializeField] private GameObject infoDisplayGroup;
    [SerializeField] private GameObject sellPriceGroup;
    [SerializeField] private Button useButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button closeButton;

    /// <summary>
    /// Unity Start – initialize slots and bind button events.
    /// </summary>
    protected override void Start()
    {
        base.Start();

        useButton.onClick.AddListener(UI_Use);
        sellButton.onClick.AddListener(UI_Sell);
        dropButton.onClick.AddListener(UI_Drop);
        closeButton.onClick.AddListener(UI_Close);
    }

    /// <summary>
    /// Called when an item is selected from inventory.
    /// Updates UI fields, icons, and available action buttons.
    /// </summary>
    protected override void OnItemSelected(ItemSlot slot, bool playFeedback)
    {
        var data = slot.ItemData;

        itemIconImage.sprite = data.icon;
        itemIconImage.gameObject.SetActive(true);

        LocalizationManager.Instance.SetLocalizedText(itemNameText, "Food Labels", data.itemNameKey);
        itemQuantityText.text = slot.Quantity.ToString();
        LocalizationManager.Instance.SetLocalizedText(itemDescriptionText, "Food Labels", data.itemDescriptionKey);
        sellPriceText.text = $"{data.SellPrice}원";

        // Toggle buttons and groups
        useButton.gameObject.SetActive(data.CanBeUsed);
        sellButton.gameObject.SetActive(data.CanBeSold);
        dropButton.gameObject.SetActive(true);
        sellPriceGroup.SetActive(data.CanBeSold);
        infoDisplayGroup.SetActive(true);

        DisplayStatUI(data);

        if (playFeedback)
            AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Called when no item is selected.
    /// Clears UI text and hides buttons/groups.
    /// </summary>
    protected override void OnItemDeselected()
    {
        itemNameText.text = "";
        itemQuantityText.text = "";
        itemDescriptionText.text = "";

        infoDisplayGroup.SetActive(false);
        useButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(false);
        dropButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when inventory is full.
    /// Displays a warning message on screen.
    /// </summary>
    protected override void OnInventoryFull(string message)
    {
        UIManager.Instance.ShowWarningText(message);
    }

    /// <summary>
    /// Handles "Use Item" action – applies stats to player.
    /// </summary>
    protected override void OnUseSelectedItem(ItemDataSO data)
    {
        var stats = playerControl.stats;
        stats.ApplyStatChange(StatType.Productivity, data.energy);
        stats.ApplyStatChange(StatType.Mood, data.mood);

        MoodManager.Instance.ClearMood(MoodConditionType.Hungry);
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Handles "Sell Item" action – increases money and plays sounds.
    /// </summary>
    protected override void OnSellSelectedItem(ItemDataSO data)
    {
        MoneyManager.Instance.ChangeMoneys(data.SellPrice);
        AudioManager.Instance.PlayInteractSound(1);
    }

    /// <summary>
    /// Handles "Drop Item" action – only plays feedback sound.
    /// </summary>
    protected override void OnDropSelectedItem(ItemDataSO data)
    {
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Handles closing the inventory – stops interaction and plays sound.
    /// </summary>
    protected override void OnCloseRequested()
    {
        playerControl.interactDetector.StopCurrentInteraction();
        AudioManager.Instance.PlayInteractSound(8);
    }

    /// <summary>
    /// Updates stat UI group to show energy/mood effects of the selected item.
    /// </summary>
    private void DisplayStatUI(ItemDataSO data)
    {
        foreach (Transform stat in statGroupRoot)
            stat.gameObject.SetActive(false);

        bool show = false;

        if (!data.canBeSold)
        {
            if (data.energy > 0)
            {
                statGroupRoot.GetChild(0).gameObject.SetActive(true);
                energyText.text = data.energy.ToString();
                show = true;
            }

            if (data.mood > 0)
            {
                statGroupRoot.GetChild(1).gameObject.SetActive(true);
                moodText.text = data.mood.ToString();
                show = true;
            }
        }

        statGroupRoot.gameObject.SetActive(show);
        StartCoroutine(RebuildLayoutNextFrame(itemInfoContainer));
    }

    /// <summary>
    /// Coroutine to rebuild UI layout in the next frame.
    /// Ensures updated stat elements are properly arranged.
    /// </summary>
    private IEnumerator RebuildLayoutNextFrame(Transform layoutRoot)
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot as RectTransform);
    }
}
