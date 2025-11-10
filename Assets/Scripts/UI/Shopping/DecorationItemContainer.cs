using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DecorationItemContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI ownerText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;

    [Header("UI Groups")]
    [SerializeField] private GameObject itemControlGroup;
    [SerializeField] private GameObject outlinedText;

    private DecorationItemSO itemData;
    private int currentQuantity = 0;
    private bool isOwned;

    private void Start()
    {
        minusButton.onClick.AddListener(OnMinusClicked);
        plusButton.onClick.AddListener(OnPlusClicked);

        UpdateUI();
    }

    public void Configure(DecorationItemSO data)
    {
        itemData = data;

        iconImage.sprite = data.icon;
        nameText.text = data.itemName;
        descriptionText.text = data.description;
        ownerText.text = $"{data.owner}";
        priceText.text = $"{data.price}$";

        currentQuantity = 0;
        isOwned = false;

        UpdateUI();
    }

    private void OnMinusClicked()
    {
        if (isOwned || currentQuantity <= 0) return;

        currentQuantity--;
        ShopManager.Instance.RefundFromTempPrice(itemData.price);
        UpdateUI();
        ShopManager.Instance.UpdateAllUI();
        AudioManager.Instance.PlayInteractSound(8);
    }

    private void OnPlusClicked()
    {
        if (isOwned || currentQuantity >= 1) return;

        if (!ShopManager.Instance.TryAddToTempPrice(itemData.price)) return;

        currentQuantity++;
        UpdateUI();
        ShopManager.Instance.UpdateAllUI();
        AudioManager.Instance.PlayInteractSound(8);
    }

    // ─────────────────────────────────────────────────────
    // Ownership & UI
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Checks if this decoration is already owned using (itemID, owner).
    /// </summary>
    private void UpdateOwnershipState()
    {
        if (itemData == null || DecorationManager.Instance == null)
        {
            isOwned = false;
            return;
        }

        isOwned = DecorationManager.Instance.IsOwned(itemData.itemID, itemData.owner);
    }

    private void ToggleUI(bool owned)
    {
        if (outlinedText != null && outlinedText.activeSelf != owned)
            outlinedText.SetActive(owned);

        if (itemControlGroup != null && itemControlGroup.activeSelf == owned)
            itemControlGroup.SetActive(!owned);
    }

    private void UpdateUI()
    {
        ToggleUI(isOwned);

        if (!isOwned)
        {
            countText.text = currentQuantity.ToString();
            minusButton.interactable = currentQuantity > 0;
            plusButton.interactable = currentQuantity < 1;
        }
    }

    public void RefreshOwnershipUI()
    {
        UpdateOwnershipState();
        UpdateUI();
    }

    public void ConfirmPurchase()
    {
        if (currentQuantity <= 0 || itemData == null) return;

        isOwned = true;
        currentQuantity = 0;

        DecorationManager.Instance.UnlockDecoration(itemData.itemID, itemData.owner);
        UpdateUI();
    }

    public void ResetSelection()
    {
        currentQuantity = 0;
        UpdateUI();
    }

    public int GetCount() => currentQuantity;
    public double GetTotalPrice() => currentQuantity * (itemData != null ? itemData.price : 0);
    public DecorationItemSO GetItemData() => itemData;
}
