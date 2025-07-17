using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // UI References
    // ─────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject plusIcon;
    [SerializeField] private GameObject highlight;
    [SerializeField] private Button selectButton;

    // ─────────────────────────────────────────────────────
    // Item Data
    // ─────────────────────────────────────────────────────
    private ItemDataSO itemData;
    private int quantity;

    // ─────────────────────────────────────────────────────
    // Public Properties
    // ─────────────────────────────────────────────────────
    public int Quantity => quantity;
    public ItemDataSO ItemData => itemData;

    public Button GetSelectButton() => selectButton;

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Set item data and quantity, then refresh the UI.
    /// </summary>
    public void SetItemData(ItemDataSO newItemData, int newQuantity)
    {
        itemData = newItemData;
        quantity = newQuantity;
        UpdateSlotUI();
    }

    /// <summary>
    /// Increase the item quantity by 1 and refresh the UI.
    /// </summary>
    public void IncreaseQuantity()
    {
        quantity++;
        UpdateSlotUI();
    }

    public void DecreaseQuantity()
    {
        quantity--;
        UpdateSlotUI();
    }

    /// <summary>
    /// Updates UI visuals for this slot based on item data and quantity.
    /// </summary>
    public void UpdateSlotUI()
    {
        if (itemData == null)
        {
            SetEmpty();
            return;
        }

        if (itemImage != null)
        {
            itemImage.sprite = itemData.icon;
            itemImage.gameObject.SetActive(true);
        }

        if (plusIcon != null)
            plusIcon.SetActive(false);

        quantityText.text = quantity > 1 ? quantity.ToString() : "";
        selectButton.interactable = true;
    }

    /// <summary>
    /// Clears the item data and shows plus icon.
    /// </summary>
    public void SetEmpty()
    {
        itemData = null;
        quantity = 0;

        if (itemImage != null)
            itemImage.gameObject.SetActive(false);

        if (plusIcon != null)
            plusIcon.SetActive(true);

        quantityText.text = "";
        selectButton.interactable = false;
    }

    /// <summary>
    /// Enable or disable selection highlight.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (highlight != null)
            highlight.SetActive(selected);
    }

    /// <summary>
    /// Reset slot to default state.
    /// </summary>
    public void ResetSlot()
    {
        SetEmpty();
        SetSelected(false);
    }

    /// <summary>
    /// Check if the slot is empty (no item data).
    /// </summary>
    public bool IsEmpty()
    {
        return itemData == null;
    }
}
