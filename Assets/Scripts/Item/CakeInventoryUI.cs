using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CakeInventoryUI : InventoryBase<CakeInventoryUI>
{
    [Header("UI Elements")]
    [SerializeField] private Transform cakeInfoContainer;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private TextMeshProUGUI slotUsageText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private GameObject infoDisplayGroup;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button closeButton;

    /// <summary>
    /// Unity Start – initialize slots and bind button events.
    /// </summary>
    protected override void Start()
    {
        base.Start();

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

        LocalizationManager.Instance.SetLocalizedText(itemNameText, "Recipe Labels", data.itemNameKey);

        LocalizationManager.Instance.SetLocalizedText(itemDescriptionText, "Recipe Labels", data.itemDescriptionKey);

        sellPriceText.text = $"{data.SellPrice}$";

        // Toggle buttons and groups
        sellButton.gameObject.SetActive(data.CanBeSold);
        dropButton.gameObject.SetActive(true);
        infoDisplayGroup.SetActive(true);

        StartCoroutine(RebuildLayoutNextFrame(cakeInfoContainer));

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
        itemDescriptionText.text = "";

        infoDisplayGroup.SetActive(false);
        sellButton.gameObject.SetActive(false);
        dropButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates slot usage UI text (e.g. "3 / 52").
    /// </summary>
    protected override void OnSlotUsageChanged(int used, int total)
    {
        slotUsageText.text = $"{used} / {total}";
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
    /// Coroutine to rebuild UI layout in the next frame.
    /// Ensures updated stat elements are properly arranged.
    /// </summary>
    private IEnumerator RebuildLayoutNextFrame(Transform layoutRoot)
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot as RectTransform);
    }
}
