using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI moodText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Transform statGroupRoot;

    private int currentQuantity = 1;
    private int itemPrice = 0;

    // Called when the object is initialized
    private void Start()
    {
        minusButton.onClick.AddListener(() => ChangeQuantity(-1));
        plusButton.onClick.AddListener(() => ChangeQuantity(1));
        UpdateQuantityUI();
    }

    // Configures the shop item UI with given item data
    public void Configure(ItemData itemData)
    {
        if (itemData == null) return;

        itemIcon.sprite = itemData.icon;
        itemName.text = itemData.itemName;
        itemDescription.text = itemData.description;
        itemPrice = itemData.price;
        priceText.text = $"{itemPrice}$";

        HandleStatDisplay(itemData);
        currentQuantity = 1;
        UpdateQuantityUI();
    }

    // Displays energy/mood stats if applicable
    private void HandleStatDisplay(ItemData itemData)
    {
        foreach (Transform stat in statGroupRoot)
            stat.gameObject.SetActive(false);

        bool hasEnergy = itemData.energy > 0;
        bool hasMood = itemData.mood > 0;

        if (hasEnergy)
        {
            statGroupRoot.GetChild(0).gameObject.SetActive(true);
            energyText.text = itemData.energy.ToString();
        }

        if (hasMood)
        {
            statGroupRoot.GetChild(1).gameObject.SetActive(true);
            moodText.text = itemData.mood.ToString();
        }

        statGroupRoot.gameObject.SetActive(hasEnergy || hasMood);
    }

    // Changes the quantity and updates the UI
    private void ChangeQuantity(int delta)
    {
        currentQuantity = Mathf.Max(1, currentQuantity + delta);
        UpdateQuantityUI();
    }

    // Updates the quantity text
    private void UpdateQuantityUI()
    {
        quantityText.text = currentQuantity.ToString();
    }

    // Returns the current quantity
    public int GetCurrentQuantity() => currentQuantity;

    // Returns the total price based on quantity
    public int GetTotalPrice() => currentQuantity * itemPrice;
}
