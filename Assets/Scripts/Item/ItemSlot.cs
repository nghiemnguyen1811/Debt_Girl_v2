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
    [SerializeField] private GameObject framequantity;
    [SerializeField] private Sprite[] spriteBtns;        // [0] = empty, [1] = filled
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
    public Button SelectButton => selectButton;

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Assign item data and quantity, then update UI.
    /// </summary>
    public void SetItemData(ItemDataSO newItemData, int newQuantity)
    {
        itemData = newItemData;
        quantity = newQuantity;
        RefreshUI();
    }

    public void IncreaseQuantity()
    {
        quantity++;
        RefreshUI();
    }

    public void DecreaseQuantity()
    {
        quantity--;
        RefreshUI();
    }

    /// <summary>
    /// Reset slot completely (empty + deselect).
    /// </summary>
    public void ResetSlot()
    {
        ClearData();
        RefreshUI();
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null)
            highlight.SetActive(selected);
    }

    public bool IsEmpty() => itemData == null;

    // ─────────────────────────────────────────────────────
    // Private Helpers
    // ─────────────────────────────────────────────────────

    private void ClearData()
    {
        itemData = null;
        quantity = 0;
    }

    /// <summary>
    /// Refresh slot UI depending on whether it has item.
    /// </summary>
    public void RefreshUI()
    {
        bool hasItem = (itemData != null);
        ApplyState(hasItem);
    }

    /// <summary>
    /// Apply UI state (filled or empty).
    /// </summary>
    private void ApplyState(bool hasItem)
    {
        // Button sprite
        if (spriteBtns != null && selectButton != null)
        {
            int index = hasItem ? 1 : 0;
            if (spriteBtns.Length > index)
                selectButton.image.sprite = spriteBtns[index];
        }

        // Item image
        if (itemImage != null)
        {
            bool showItem = hasItem && itemData != null;
            itemImage.gameObject.SetActive(showItem);
            if (showItem) itemImage.sprite = itemData.icon;
        }

        // Plus icon & Frame quantity
        if (plusIcon != null) plusIcon.SetActive(!hasItem);
        if (framequantity != null) framequantity.SetActive(hasItem);

        // Quantity text
        if (quantityText != null)
            quantityText.text = quantity.ToString();

        // Button interaction
        if (selectButton != null)
            selectButton.interactable = hasItem;
    }
}
