using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Localization;

public class FoodItemContainer : MonoBehaviour, ILocalizableContainer
{
    [Header("UI References")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemPriceText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI moodText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Transform statGroupRoot;

    private ItemDataSO itemData;
    private int currentQuantity;
    private double itemPrice;

    //─────────────────────────────────────────────────────
    // Mono
    //─────────────────────────────────────────────────────
    private void Start()
    {
        plusButton.onClick.AddListener(OnPlusClicked);
        minusButton.onClick.AddListener(OnMinusClicked);
        UpdateQuantityUI();
    }

    //─────────────────────────────────────────────────────
    // Setup
    //─────────────────────────────────────────────────────
    public void Configure(ItemDataSO data)
    {
        itemData = data;
        currentQuantity = 0;

        if (itemData == null) return;

        itemIconImage.sprite = data.icon;
        itemPrice = data.purchaseCost;

        RefreshLocalizedTexts();
        RefreshLocalizedPrice();
        DisplayStatUI(data);
        UpdateQuantityUI();
    }

    //─────────────────────────────────────────────────────
    // Localization
    //─────────────────────────────────────────────────────
    public void RefreshLocalizedTexts()
    {
        if (itemData == null) return;

        LocalizationManager.Instance.SetLocalizedText(itemNameText, "Food Labels", itemData.itemNameKey);
        LocalizationManager.Instance.SetLocalizedText(itemDescriptionText, "Food Labels", itemData.itemDescriptionKey);
    }

    public void RefreshLocalizedPrice()
    {
        string localizedSymbol = LocalizationManager.Instance.GetCurrencySymbol();
        itemPriceText.text = $"{DoubleUtilities.ToIdleNotation(itemPrice)}{localizedSymbol}";
    }

    private void DisplayStatUI(ItemDataSO data)
    {
        foreach (Transform stat in statGroupRoot)
            stat.gameObject.SetActive(false);

        if (data.itemType == ItemType.Material) return;

        bool hasEnergy = data.energy > 0;
        bool hasMood = data.mood > 0;

        if (hasEnergy)
        {
            statGroupRoot.GetChild(0).gameObject.SetActive(true);
            energyText.text = data.energy.ToString();
        }

        if (hasMood)
        {
            statGroupRoot.GetChild(1).gameObject.SetActive(true);
            moodText.text = data.mood.ToString();
        }

        statGroupRoot.gameObject.SetActive(hasEnergy || hasMood);
    }

    //─────────────────────────────────────────────────────
    // Button Handlers
    //─────────────────────────────────────────────────────
    private void OnPlusClicked()
    {
        if (!ShopManager.Instance.TryAddToTempPrice(itemPrice)) return;

        currentQuantity++;
        UpdateQuantityUI();
        ShopManager.Instance.UpdateAllUI();
        AudioManager.Instance.PlayInteractSound(8);
    }

    private void OnMinusClicked()
    {
        if (currentQuantity <= 0) return;

        currentQuantity--;
        ShopManager.Instance.RefundFromTempPrice(itemPrice);
        UpdateQuantityUI();
        ShopManager.Instance.UpdateAllUI();
        AudioManager.Instance.PlayInteractSound(8);
    }

    //─────────────────────────────────────────────────────
    // UI Update
    //─────────────────────────────────────────────────────
    private void UpdateQuantityUI()
    {
        quantityText.text = currentQuantity.ToString();
    }

    public void UpdateButtonStates()
    {
        plusButton.interactable = ShopManager.Instance.HasSufficientFunds(itemPrice);
        minusButton.interactable = currentQuantity > 0;
    }

    //─────────────────────────────────────────────────────
    // Purchase Logic
    //─────────────────────────────────────────────────────
    public void ConfirmPurchase()
    {
        if (currentQuantity <= 0) return;

        currentQuantity = 0;
        UpdateQuantityUI();
    }

    public void ResetSelection()
    {
        currentQuantity = 0;
        UpdateQuantityUI();
    }

    //─────────────────────────────────────────────────────
    // Accessors
    //─────────────────────────────────────────────────────
    public int GetCurrentQuantity() => currentQuantity;
    public double GetTotalPrice() => currentQuantity * itemPrice;
    public ItemDataSO GetItemData() => itemData;
}
