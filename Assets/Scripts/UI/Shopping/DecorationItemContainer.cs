using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Handles display, interaction, and localization for each decoration item in the shop.
/// </summary>
public class DecorationItemContainer : MonoBehaviour, ILocalizableContainer
{
    //─────────────────────────────────────────────────────
    // UI References
    //─────────────────────────────────────────────────────
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

    //─────────────────────────────────────────────────────
    // Private Data
    //─────────────────────────────────────────────────────
    private DecorationItemSO itemData;
    private int currentQuantity;
    private bool isOwned;

    //─────────────────────────────────────────────────────
    // Unity Lifecycle
    //─────────────────────────────────────────────────────
    private void Start()
    {
        plusButton.onClick.AddListener(OnPlusClicked);
        minusButton.onClick.AddListener(OnMinusClicked);
        UpdateUI();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.RegisterForGlobalRefresh(RefreshLocalizedTexts);
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.UnregisterForGlobalRefresh(RefreshLocalizedTexts);
    }

    //─────────────────────────────────────────────────────
    // Setup
    //─────────────────────────────────────────────────────
    public void Configure(DecorationItemSO data)
    {
        itemData = data;
        currentQuantity = 0;
        isOwned = false;

        if (itemData == null) return;

        iconImage.sprite = data.icon;

        RefreshLocalizedTexts();
        RefreshLocalizedPrice();
        UpdateUI();
    }

    //─────────────────────────────────────────────────────
    // Localization
    //─────────────────────────────────────────────────────
    public void RefreshLocalizedTexts()
    {
        if (itemData == null) return;
        
        LocalizationManager.Instance.SetLocalizedText(nameText, "Decoration Labels", itemData.decorationNameKey);
        LocalizationManager.Instance.SetLocalizedText(descriptionText, "Decoration Labels", itemData.decorationDescriptionKey);

        // Owner name is usually enum (CharacterType), convert to string directly
        ownerText.text = itemData.owner.ToString();
    }

    public void RefreshLocalizedPrice()
    {
        string localizedSymbol = LocalizationManager.Instance.GetCurrencySymbol();
        priceText.text = $"{DoubleUtilities.ToIdleNotation(itemData.price)}{localizedSymbol}";
    }

    //─────────────────────────────────────────────────────
    // Ownership & UI
    //─────────────────────────────────────────────────────
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
        if (outlinedText != null)
            outlinedText.SetActive(owned);

        if (itemControlGroup != null)
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

    //─────────────────────────────────────────────────────
    // Button Handlers
    //─────────────────────────────────────────────────────
    private void OnPlusClicked()
    {
        if (isOwned || currentQuantity >= 1) return;
        if (!ShopManager.Instance.TryAddToTempPrice(itemData.price)) return;

        currentQuantity++;
        UpdateUI();
        ShopManager.Instance.UpdateAllUI();
        AudioManager.Instance.PlayInteractSound(8);
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

    //─────────────────────────────────────────────────────
    // Purchase Logic
    //─────────────────────────────────────────────────────
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

    //─────────────────────────────────────────────────────
    // Accessors
    //─────────────────────────────────────────────────────
    public int GetCount() => currentQuantity;
    public double GetTotalPrice() => currentQuantity * (itemData != null ? itemData.price : 0);
    public DecorationItemSO GetItemData() => itemData;
}
