using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private int currentCount = 0;
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

        currentCount = 0;
        isOwned = false;
        UpdateUI();
    }

    private void OnMinusClicked()
    {
        if (isOwned || currentCount <= 0) return;

        currentCount--;
        ShopManager.Instance.RefundFromTempPrice(itemData.price);
        UpdateUI();
        ShopManager.Instance.UpdateAllUI();
        AudioManager.Instance.PlayInteractSound(8);
    }

    private void OnPlusClicked()
    {
        if (isOwned || currentCount >= 1) return;

        if (!ShopManager.Instance.TryAddToTempPrice(itemData.price)) return;

        currentCount++;
        UpdateUI();
        ShopManager.Instance.UpdateAllUI();
        AudioManager.Instance.PlayInteractSound(8);
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
            countText.text = currentCount.ToString();
            minusButton.interactable = currentCount > 0;
            plusButton.interactable = currentCount < 1;
        }
    }

    public void ConfirmPurchase()
    {
        if (currentCount <= 0) return;

        isOwned = true;
        UpdateUI();
    }

    public int GetCount() => currentCount;
    public double GetTotalPrice() => currentCount * (itemData != null ? itemData.price : 0);
    public DecorationItemSO GetItemData() => itemData;
}
