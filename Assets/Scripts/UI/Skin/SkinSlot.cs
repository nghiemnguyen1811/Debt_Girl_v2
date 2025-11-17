using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class SkinSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // 🔗 UI REFERENCES (Inspector)
    // ─────────────────────────────────────────────────────
    [Title("UI References", bold: true)]
    [SerializeField] private Image skinImage;
    [SerializeField] private Image characterIcon;
    [SerializeField] private TMP_Text[] priceTexts;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button unlockButtonEnabled;
    [SerializeField] private Button unlockButtonDisabled;
    [SerializeField] private GameObject lockedOverlay;

    // ─────────────────────────────────────────────────────
    // 🎨 HIGHLIGHT SPRITES (Inspector)
    // ─────────────────────────────────────────────────────
    [Title("Highlight Sprites", bold: true)]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite equippedSprite;

    //─────────────────────────────────────────────
    // 🎨 CHARACTER ICONS ===
    //─────────────────────────────────────────────
    [Title("Character Icons", bold: true)]
    [SerializeField] private Sprite[] characterIcons;

    // ─────────────────────────────────────────────────────
    // 📦 RUNTIME DATA
    // ─────────────────────────────────────────────────────
    private SkinDataSO skinData;
    private bool isUnlocked;
    private bool isEquipped;

    // ─────────────────────────────────────────────────────
    // 🚀 INITIALIZATION
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Initialize this slot with skin data and initial unlocked/equipped states.
    /// </summary>
    public void Setup(SkinDataSO data, bool unlocked = false, bool equipped = false)
    {
        skinData = data;
        isUnlocked = unlocked;
        isEquipped = equipped;

        if (skinImage) skinImage.sprite = data.icon;
        if (characterIcon) characterIcon.sprite = GetCharacterIcon(data.owner);

        foreach (var text in priceTexts)
            if (text != null)
                text.text = "x " + data.sellPrice;

        UpdateLockState(!isUnlocked);
        UpdateVisualState();

        selectButton.onClick.RemoveAllListeners();
        unlockButtonEnabled.onClick.RemoveAllListeners();

        selectButton.onClick.AddListener(OnSelected);
        unlockButtonEnabled.onClick.AddListener(OnUnlockClicked);

        // Initial unlock check based on current diamond amount
        RefreshUnlockState();
    }

    // ─────────────────────────────────────────────────────
    // 💎 UNLOCK STATE HANDLING
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Refresh the state of unlock buttons based on player's diamonds.
    /// </summary>
    public void RefreshUnlockState()
    {
        if (unlockButtonEnabled == null || isUnlocked || skinData == null)
            return;

        double currentDiamonds = MoneyManager.Instance != null ? MoneyManager.Instance.GetDiamonds() : 0;
        bool canAfford = currentDiamonds >= skinData.sellPrice;

        unlockButtonEnabled.gameObject.SetActive(canAfford);
        unlockButtonDisabled.gameObject.SetActive(!canAfford);
    }

    /// <summary>
    /// Set unlocked flag from external systems (e.g., after loading save).
    /// </summary>
    public void SetUnlock(bool unlocked)
    {
        isUnlocked = unlocked;
        UpdateLockState(!isUnlocked);
        UpdateVisualState();
    }

    // ─────────────────────────────────────────────────────
    // 🧩 INTERACTION
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Called when the slot is selected; notifies OutfitManager if valid.
    /// </summary>
    private void OnSelected()
    {
        if (!isUnlocked || isEquipped) return;

        OutfitManager.Instance.OnSkinSelected(this);
        AudioManager.Instance.PlayInteractSound(8);

        SetSelected(true);
    }

    /// <summary>
    /// Try to unlock this skin by spending diamonds.
    /// </summary>
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

            AudioManager.Instance.PlayInteractSound(15);
            Debug.Log($"✅ Unlocked skin: {skinData.name} for {price}💎");
        }

        else Debug.LogWarning("❌ Not enough diamonds to unlock this skin!");
    }

    // ─────────────────────────────────────────────────────
    // 🎨 VISUAL STATE
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Visualize selected state (ignored if currently equipped).
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectButton == null || selectButton.image == null)
            return;

        if (isEquipped) selectButton.image.sprite = equippedSprite;
        else selectButton.image.sprite = isSelected ? selectedSprite : normalSprite;
    }

    /// <summary>
    /// Apply equipped flag and refresh visuals.
    /// </summary>
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
        characterIcon.gameObject.SetActive(!equipped);
        UpdateVisualState();
    }

    /// <summary>
    /// Update button sprite and interactable depending on equipped/unlocked.
    /// </summary>
    private void UpdateVisualState()
    {
        if (selectButton == null || selectButton.image == null)
            return;

        // Update button sprite
        selectButton.image.sprite = isEquipped ? equippedSprite : normalSprite;

        // Disable interaction if equipped or locked
        selectButton.interactable = !isEquipped && isUnlocked;
    }

    /// <summary>
    /// Toggle locked overlay and clickability.
    /// </summary>
    public void UpdateLockState(bool isLocked)
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(isLocked);

        if (selectButton != null)
            selectButton.interactable = !isLocked && !isEquipped;
    }

    //─────────────────────────────────────────────
    // 🧩 Helper
    //─────────────────────────────────────────────
    /// <summary>
    /// Returns the matching icon for a given character type.
    /// </summary>
    private Sprite GetCharacterIcon(CharacterType type)
    {
        int index = (int)type;

        if (characterIcons == null || index < 0 || index >= characterIcons.Length)
            return null;
        return characterIcons[index - 1];
    }

    // ─────────────────────────────────────────────────────
    // 🧾 ACCESSORS
    // ─────────────────────────────────────────────────────
    public SkinDataSO SkinData => skinData;
    public bool IsUnlocked => isUnlocked;
    public bool IsEquipped => isEquipped;
}
