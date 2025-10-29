using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class SkinSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // 🔗 UI References
    // ─────────────────────────────────────────────────────
    [Title("UI References", bold: true)]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text[] priceTexts;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button unlockButtonEnabled;
    [SerializeField] private Button unlockButtonDisabled;
    [SerializeField] private GameObject lockedOverlay;

    // ─────────────────────────────────────────────────────
    // 🎨 Highlight Sprites
    // ─────────────────────────────────────────────────────
    [Title("Highlight Sprites", bold: true)]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite equippedSprite;

    // ─────────────────────────────────────────────────────
    // 📦 Data
    // ─────────────────────────────────────────────────────
    private SkinDataSO skinData;
    private bool isUnlocked = false;
    private bool isEquipped = false;

    // ─────────────────────────────────────────────────────
    // 🚀 Initialization
    // ─────────────────────────────────────────────────────
    public void Setup(SkinDataSO data, bool unlocked = false, bool equipped = false)
    {
        skinData = data;
        isUnlocked = unlocked;
        isEquipped = equipped;

        if (iconImage != null)
            iconImage.sprite = data.icon;

        foreach (var text in priceTexts)
            if (text != null)
                text.text = data.sellPrice.ToString();

        UpdateLockState(!isUnlocked);
        UpdateVisualState();

        selectButton.onClick.RemoveAllListeners();
        unlockButtonEnabled.onClick.RemoveAllListeners();

        selectButton.onClick.AddListener(OnSelected);
        unlockButtonEnabled.onClick.AddListener(OnUnlockClicked);

        // 🔹 Initial unlock check
        RefreshUnlockState();
    }

    // ─────────────────────────────────────────────────────
    // 💎 Unlock State Handling
    // ─────────────────────────────────────────────────────
    public void RefreshUnlockState()
    {
        if (unlockButtonEnabled == null || isUnlocked || skinData == null)
            return;

        double currentDiamonds = MoneyManager.Instance != null ? MoneyManager.Instance.GetDiamonds() : 0;
        bool canAfford = currentDiamonds >= skinData.sellPrice;

        unlockButtonEnabled.gameObject.SetActive(canAfford);
        unlockButtonDisabled.gameObject.SetActive(!canAfford);
    }

    // ─────────────────────────────────────────────────────
    // 🧩 Interaction
    // ─────────────────────────────────────────────────────
    private void OnSelected()
    {
        if (!isUnlocked || isEquipped) return;

        OutfitManager.Instance.OnSkinSelected(this);
        SetSelected(true);
    }

    private void OnUnlockClicked()
    {
        if (isUnlocked || skinData == null)
            return;

        double price = skinData.sellPrice;

        if (MoneyManager.Instance.HasEnoughDiamond(price))
        {
            MoneyManager.Instance.ChangeDiamonds(-price);
            OutfitManager.Instance.UnlockSkin(skinData);

            isUnlocked = true;
            UpdateLockState(false);
            UpdateVisualState();

            Debug.Log($"✅ Unlocked skin: {skinData.name} for {price}💎");
        }

        else Debug.LogWarning("❌ Not enough diamonds to unlock this skin!");
    }

    // ─────────────────────────────────────────────────────
    // 🎨 Visual State
    // ─────────────────────────────────────────────────────
    public void SetSelected(bool isSelected)
    {
        if (selectButton == null || selectButton.image == null)
            return;

        if (isEquipped) selectButton.image.sprite = equippedSprite;
        else selectButton.image.sprite = isSelected ? selectedSprite : normalSprite;
    }

    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (selectButton == null || selectButton.image == null)
            return;

        // 🔹 Update button sprite
        selectButton.image.sprite = isEquipped ? equippedSprite : normalSprite;

        // 🔹 Disable interaction if equipped or locked
        selectButton.interactable = !isEquipped && isUnlocked;
    }

    public void UpdateLockState(bool isLocked)
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(isLocked);

        if (selectButton != null)
            selectButton.interactable = !isLocked && !isEquipped;
    }

    // ─────────────────────────────────────────────────────
    // 🧾 Accessors
    // ─────────────────────────────────────────────────────
    public SkinDataSO SkinData => skinData;
    public bool IsUnlocked => isUnlocked;
    public bool IsEquipped => isEquipped;
}
