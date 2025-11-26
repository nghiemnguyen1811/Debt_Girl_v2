using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all outfit logic: skin ownership, equipping, UI display, and saving.
/// </summary>
public class OutfitManager : SingletonMonobehaviour<OutfitManager>
{
    // ══════════════════════════════════════════════════════
    // 🔧 Inspector Fields
    // ══════════════════════════════════════════════════════
    [Header("Skin Slot References")]
    [SerializeField] private Transform shirtParent;
    [SerializeField] private Transform pantParent;
    [SerializeField] private Transform shoesParent;
    [SerializeField] private SkinSlot skinSlotPrefab;

    [Header("All Skin Data")]
    [SerializeField] private List<SkinDataSO> allSkins = new();

    [Header("Character Tabs")]
    [SerializeField] private Transform characterTabParent;
    [SerializeField] private CharacterTabButton characterTabPrefab;
    [SerializeField] private List<CharacterInfoSO> characterTabList;

    [Header("Outfit Tabs")]
    [SerializeField] private List<Tab> outfitTabs; // Hat / Top / Shoes tabs

    [Header("Equip Button")]
    [SerializeField] private Button equipButtonEnabled;
    [SerializeField] private Button equipButtonDisabled;

    [Header("Equipped Preview Images")]
    [SerializeField] private Image equippedShirtImage;
    [SerializeField] private Image equippedPantImage;
    [SerializeField] private Image equippedShoesImage;

    [Header("Preview & Reset Buttons")]
    [SerializeField] private Button resetRotationButton;
    [SerializeField] private Button resetDefaultSkinButton;
    [SerializeField] private PreviewRotator previewRotator;


    // ══════════════════════════════════════════════════════
    // 🧠 Runtime Data
    // ══════════════════════════════════════════════════════
    private readonly List<CharacterTabButton> spawnedCharacterTabs = new();
    private readonly List<SkinSlot> spawnedShirtSlots = new();
    private readonly List<SkinSlot> spawnedPantSlots = new();
    private readonly List<SkinSlot> spawnedShoesSlots = new();
    private readonly List<SkinSlot> allSkinSlots = new();

    private CharacterTabButton currentSelectedTab;
    private Tab currentActiveTab;
    private CharacterType currentCharacter;
    private SkinSlot currentSelectedSlot;

    // ══════════════════════════════════════════════════════
    // 💾 Save Data Cache
    // ══════════════════════════════════════════════════════
    private List<string> unlockedSkins = new();
    private List<EquippedOutfitEntry> equippedOutfits = new();

    // ══════════════════════════════════════════════════════
    // 🏁 Unity Lifecycle
    // ══════════════════════════════════════════════════════

    private void OnEnable()
    {
        InitializeCharacterTabs();
        InitializeOutfitUI();
        SetupTabs();
        InitializeEvents();
        InitializeCharacterState();

        UpdateEquipButtonState(false);
    }

    /// <summary>
    /// Registers all necessary event listeners.
    /// </summary>
    private void InitializeEvents()
    {
        CharacterTabButton.OnTabSelected += HandleCharacterTabSelected;

        if (PlayerControl.Instance != null)
            PlayerControl.Instance.OnCharacterProfileChanged += HandleCharacterChanged;

        if (equipButtonEnabled != null)
            equipButtonEnabled.onClick.AddListener(OnEquipButtonClicked);

        if (resetRotationButton != null)
            resetRotationButton.onClick.AddListener(ResetModelRotation);

        if (resetDefaultSkinButton != null)
            resetDefaultSkinButton.onClick.AddListener(ResetToDefaultSkins);
    }

    /// <summary>
    /// Clean up events to prevent leaks when the object is destroyed.
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();

        CharacterTabButton.OnTabSelected -= HandleCharacterTabSelected;

        if (PlayerControl.Instance != null)
            PlayerControl.Instance.OnCharacterProfileChanged -= HandleCharacterChanged;

        if (equipButtonEnabled != null)
            equipButtonEnabled.onClick.RemoveListener(OnEquipButtonClicked);

        if (resetRotationButton != null)
            resetRotationButton.onClick.RemoveListener(ResetModelRotation);

        if (resetDefaultSkinButton != null)
            resetDefaultSkinButton.onClick.RemoveListener(ResetToDefaultSkins);
    }

    /// <summary>
    /// Initializes the active tab and sets the current character.
    /// </summary>
    private void InitializeCharacterState()
    {
        if (outfitTabs.Count > 0)
            ActivateTab(outfitTabs[1]);

        if (PlayerControl.Instance != null && PlayerControl.Instance.CharacterProfile != null)
            currentCharacter = PlayerControl.Instance.CharacterProfile.characterType;
    }

    // ══════════════════════════════════════════════════════
    // 👤 Character Tabs
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Creates all character tab buttons in the UI.
    /// </summary>
    private void InitializeCharacterTabs()
    {
        foreach (var tabData in characterTabList)
        {
            var tab = Instantiate(characterTabPrefab, characterTabParent);
            tab.Configure(tabData.avatarIcon, tabData.characterType);
            tab.SetSelected(false);
            spawnedCharacterTabs.Add(tab);
        }
    }

    /// <summary>
    /// Handles when a character tab is clicked.
    /// </summary>
    private void HandleCharacterTabSelected(CharacterType selectedType)
    {
        // If same tab clicked again → deselect and show all outfits
        if (currentSelectedTab != null && currentSelectedTab.CharacterType == selectedType)
        {
            currentSelectedTab.SetSelected(false);
            currentSelectedTab = null;
            ShowAllOutfits();
            return;
        }

        // Deselect previous tab if exists
        currentSelectedTab?.SetSelected(false);

        // Find and select new tab
        currentSelectedTab = spawnedCharacterTabs.Find(t => t.CharacterType == selectedType);
        if (currentSelectedTab == null)
            return;

        currentSelectedTab.SetSelected(true);
        SetCurrentCharacter(selectedType);
    }


    /// <summary>
    /// Called when the player's active character changes.
    /// </summary>
    private void HandleCharacterChanged(CharacterInfoSO newProfile)
    {
        if (newProfile == null)
            return;

        currentCharacter = newProfile.characterType;
        RefreshEquippedVisuals();

        bool canEquip = false;

        if (currentSelectedSlot != null)
        {
            if (currentSelectedSlot.SkinData.owner != currentCharacter)
            {
                currentSelectedSlot.SetSelected(false);
                currentSelectedSlot = null;
            }

            else canEquip = currentSelectedSlot.IsUnlocked;
        }

        UpdateEquipButtonState(canEquip);
        Debug.Log($"👤 Character switched to: {currentCharacter}. Refreshed equipped outfits.");
    }

    /// <summary>
    /// Shows all outfit slots (for all characters).
    /// </summary>
    private void ShowAllOutfits()
    {
        foreach (var slot in allSkinSlots)
            slot.gameObject.SetActive(true);
    }

    /// <summary>
    /// Sets the current character and refreshes outfit visibility.
    /// </summary>
    private void SetCurrentCharacter(CharacterType character)
    {
        currentCharacter = character;
        FilterByCharacter(character);
    }

    /// <summary>
    /// Hides outfits that don't belong to the active character.
    /// </summary>
    private void FilterByCharacter(CharacterType type)
    {
        void FilterList(List<SkinSlot> list)
        {
            foreach (var slot in list)
                slot.gameObject.SetActive(slot.SkinData.owner == type);
        }

        FilterList(spawnedShirtSlots);
        FilterList(spawnedPantSlots);
        FilterList(spawnedShoesSlots);
    }

    // ══════════════════════════════════════════════════════
    // 🎨 Outfit UI Setup
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Creates outfit slot UI for each outfit type.
    /// </summary>
    private void InitializeOutfitUI()
    {
        var shirtList = allSkins.Where(s => s.skinType == OutfitType.Shirt).ToList();
        var pantList = allSkins.Where(s => s.skinType == OutfitType.Pant).ToList();
        var shoesList = allSkins.Where(s => s.skinType == OutfitType.Shoes).ToList();

        SpawnSlots(shirtList, shirtParent, spawnedShirtSlots);
        SpawnSlots(pantList, pantParent, spawnedPantSlots);
        SpawnSlots(shoesList, shoesParent, spawnedShoesSlots);
    }

    /// <summary>
    /// Spawns individual skin slots inside a parent container.
    /// </summary>
    private void SpawnSlots(List<SkinDataSO> dataList, Transform parent, List<SkinSlot> spawnedList)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);

        spawnedList.Clear();

        foreach (var data in dataList)
        {
            var slot = Instantiate(skinSlotPrefab, parent);
            bool unlocked = unlockedSkins.Contains(data.name) ||
                data.isDefaultSkin;
            slot.Setup(data, unlocked);
            spawnedList.Add(slot);
            allSkinSlots.Add(slot);
        }
    }

    // ══════════════════════════════════════════════════════
    // 🧥 Outfit Tabs
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Sets up the Hat/Top/Shoes tabs in the UI.
    /// </summary>
    private void SetupTabs()
    {
        foreach (var tab in outfitTabs)
        {
            tab.button.onClick.AddListener(() => ActivateTab(tab));
            SetTabVisual(tab, false);
            if (tab.group != null)
                tab.group.SetActive(false);
        }
    }

    /// <summary>
    /// Switches between tabs.
    /// </summary>
    private void ActivateTab(Tab tab)
    {
        if (currentActiveTab != null)
        {
            SetTabVisual(currentActiveTab, false);
            if (currentActiveTab.group != null)
                currentActiveTab.group.SetActive(false);
        }

        currentActiveTab = tab;
        SetTabVisual(tab, true);
        if (tab.group != null)
            tab.group.SetActive(true);
    }

    /// <summary>
    /// Updates tab highlight visuals.
    /// </summary>
    private void SetTabVisual(Tab tab, bool isActive)
    {
        if (tab.icon != null)
            tab.icon.color = isActive
                ? Color.black : new Color32(0xD9, 0xD9, 0xD9, 0xFF);

        if (tab.outline != null)
            tab.outline.SetActive(isActive);
    }

    // ══════════════════════════════════════════════════════
    // 🎯 Skin Selection & Equip Button
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Called when a skin is selected in the UI.
    /// </summary>
    public void OnSkinSelected(SkinSlot selectedSkin)
    {
        foreach (var slot in allSkinSlots)
            if (!slot.IsEquipped)
                slot.SetSelected(false);

        currentSelectedSlot = selectedSkin;

        bool canEquip = currentSelectedSlot != null &&
                        currentSelectedSlot.IsUnlocked &&
                        selectedSkin.SkinData.owner == currentCharacter;


        UpdateEquipButtonState(canEquip);
    }

    /// <summary>
    /// Enables or disables the equip button.
    /// </summary>
    private void UpdateEquipButtonState(bool active)
    {
        if (equipButtonEnabled == null || equipButtonDisabled == null)
            return;

        equipButtonEnabled.gameObject.SetActive(active);
        equipButtonDisabled.gameObject.SetActive(!active);
    }

    /// <summary>
    /// Called when the equip button is clicked.
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (currentSelectedSlot == null || !currentSelectedSlot.IsUnlocked)
        {
            Debug.LogWarning("No skin selected or skin not unlocked!");
            return;
        }

        EquipSkin(currentSelectedSlot.SkinData);
        AudioManager.Instance.PlayInteractSound(8);
    }

    // ══════════════════════════════════════════════════════
    // 👕 Equip / Unequip Logic
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Equips a new skin and saves it to the character's data.
    /// </summary>
    public void EquipSkin(SkinDataSO newSkin)
    {
        if (currentCharacter == CharacterType.All)
            return;

        List<SkinSlot> targetList = GetSlotList(newSkin.skinType);
        if (targetList == null) return;

        foreach (var slot in targetList)
            slot.SetEquipped(false);

        var targetSlot = targetList.Find(s => s.SkinData == newSkin);
        if (targetSlot != null)
            targetSlot.SetEquipped(true);

        equippedOutfits.RemoveAll(e => e.owner == currentCharacter && e.outfitType == newSkin.skinType);
        equippedOutfits.Add(new EquippedOutfitEntry
        {
            owner = currentCharacter,
            outfitType = newSkin.skinType,
            skinID = newSkin.name
        });

        AutoSave();
        UpdateEquippedPreviewImages();
        UpdateEquipButtonState(false);
        ApplyCurrentOutfitsToPlayer();
        Debug.Log($"[{currentCharacter}] equipped {newSkin.skinType}: {newSkin.name}");
    }

    /// <summary>
    /// Returns the correct slot list by outfit type.
    /// </summary>
    private List<SkinSlot> GetSlotList(OutfitType type)
    {
        return type switch
        {
            OutfitType.Shirt => spawnedShirtSlots,
            OutfitType.Pant => spawnedPantSlots,
            OutfitType.Shoes => spawnedShoesSlots,
            _ => null
        };
    }

    // ══════════════════════════════════════════════════════
    // 🎭 Equipped Previews
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Updates the preview images for equipped hat, top, and shoes.
    /// </summary>
    private void UpdateEquippedPreviewImages()
    {
        if (currentCharacter == CharacterType.All)
            return;

        if (equippedOutfits == null || equippedOutfits.Count == 0)
            return;

        var equippedShirt = equippedOutfits.FirstOrDefault(e => e.owner == currentCharacter && e.outfitType == OutfitType.Shirt);
        var equippedPant = equippedOutfits.FirstOrDefault(e => e.owner == currentCharacter && e.outfitType == OutfitType.Pant);
        var equippedShoes = equippedOutfits.FirstOrDefault(e => e.owner == currentCharacter && e.outfitType == OutfitType.Shoes);

        Sprite shirtIcon = GetSkinIconByID(equippedShirt.skinID);
        Sprite pantIcon = GetSkinIconByID(equippedPant.skinID);
        Sprite shoesIcon = GetSkinIconByID(equippedShoes.skinID);

        if (equippedShirtImage != null) equippedShirtImage.sprite = shirtIcon;
        if (equippedPantImage != null) equippedPantImage.sprite = pantIcon;
        if (equippedShoesImage != null) equippedShoesImage.sprite = shoesIcon;
    }

    /// <summary>
    /// Finds and returns the sprite icon for a skin by its name.
    /// </summary>
    private Sprite GetSkinIconByID(string skinID)
    {
        if (string.IsNullOrEmpty(skinID))
            return null;

        var skin = allSkins.FirstOrDefault(s => s.name == skinID);
        return skin != null ? skin.icon : null;
    }

    // ══════════════════════════════════════════════════════
    // 💎 Unlock Management
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Refreshes the state of all unlock buttons.
    /// </summary>
    public void RefreshUnlockButtons()
    {
        UpdateList(spawnedShirtSlots);
        UpdateList(spawnedPantSlots);
        UpdateList(spawnedShoesSlots);
    }

    private void UpdateList(List<SkinSlot> slots)
    {
        foreach (var slot in slots)
            slot.RefreshUnlockState();
    }

    // ══════════════════════════════════════════════════════
    // 🔄 Visual Updates
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Updates the equipped state visuals for all outfit slots.
    /// </summary>
    private void RefreshEquippedVisuals()
    {
        if (currentCharacter == CharacterType.All)
            return;

        foreach (var entry in equippedOutfits.Where(e => e.owner == currentCharacter))
        {
            switch (entry.outfitType)
            {
                case OutfitType.Shirt: ApplyEquippedVisual(spawnedShirtSlots, entry.skinID); break;
                case OutfitType.Pant: ApplyEquippedVisual(spawnedPantSlots, entry.skinID); break;
                case OutfitType.Shoes: ApplyEquippedVisual(spawnedShoesSlots, entry.skinID); break;
            }
        }

        UpdateEquippedPreviewImages();
        ApplyCurrentOutfitsToPlayer();
    }

    private void ApplyEquippedVisual(List<SkinSlot> slots, string skinID)
    {
        foreach (var slot in slots)
            slot.SetEquipped(slot.SkinData.name == skinID);
    }

    // ══════════════════════════════════════════════════════
    // 💾 Save / Load System
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Imports saved data for owned and equipped skins.
    /// </summary>
    public void ImportSaveData(SaveData data)
    {
        CleanDestroyedSlots();

        unlockedSkins = data.unlockedSkins ?? new List<string>();
        equippedOutfits = data.equippedOutfits ?? new List<EquippedOutfitEntry>();

        if (equippedOutfits.Count == 0)
            AutoEquipDefaultFreeSkins();

        foreach (var slot in allSkinSlots)
        {
            bool isUnlocked = unlockedSkins.Contains(slot.SkinData.name) ||
                slot.SkinData.isDefaultSkin;

            slot.SetUnlock(isUnlocked);
        }

        RefreshEquippedVisuals();
        Debug.Log("[OutfitManager] Save data imported. Skins and equips refreshed.");
    }

    /// <summary>
    /// Saves current owned and equipped skins to SaveManager.
    /// </summary>
    private void AutoSave()
    {
        SaveManager.Data.unlockedSkins = new List<string>(unlockedSkins);
        SaveManager.Data.equippedOutfits = new List<EquippedOutfitEntry>(equippedOutfits);
    }

    /// <summary>
    /// Automatically equips all default/free skins on first load.
    /// </summary>
    private void AutoEquipDefaultFreeSkins()
    {
        var defaultOrFreeSkins = allSkins
            .Where(s => s.isDefaultSkin || s.sellPrice == 0)
            .ToList();

        foreach (var skin in defaultOrFreeSkins)
        {
            if (!unlockedSkins.Contains(skin.name))
                unlockedSkins.Add(skin.name);

            bool alreadyEquipped = equippedOutfits.Any(e =>
                e.owner == skin.owner && e.outfitType == skin.skinType);

            if (!alreadyEquipped)
            {
                equippedOutfits.Add(new EquippedOutfitEntry
                {
                    owner = skin.owner,
                    outfitType = skin.skinType,
                    skinID = skin.name
                });

                Debug.Log($"[OutfitManager] Auto equipped {skin.name} ({skin.owner}, {skin.skinType})");
            }
        }

        AutoSave();
    }

    // ══════════════════════════════════════════════════════
    // 🔓 Unlock System
    // ══════════════════════════════════════════════════════
    /// <summary>
    /// Unlocks a new skin and saves it.
    /// </summary>
    public void UnlockSkin(SkinDataSO skin)
    {
        if (!unlockedSkins.Contains(skin.name))
            unlockedSkins.Add(skin.name);

        AutoSave();
    }

    /// <summary>
    /// Checks if a skin has already been unlocked.
    /// </summary>
    public bool IsSkinUnlocked(SkinDataSO skin)
    {
        return unlockedSkins.Contains(skin.name);
    }

    // ══════════════════════════════════════════════════════
    // 🧩 Helper
    // ══════════════════════════════════════════════════════
    private void ApplyCurrentOutfitsToPlayer()
    {
        if (PlayerControl.Instance == null || PlayerControl.Instance.outfitVisualizer == null)
            return;

        PlayerControl.Instance.outfitVisualizer.ApplyOutfits(currentCharacter, equippedOutfits);
    }

    // ══════════════════════════════════════════════════════
    // 🧹 Cleanup Destroyed UI Slots (Prevent Null-Reference)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Removes destroyed SkinSlot objects from all slot lists.
    /// </summary>
    private void CleanDestroyedSlots()
    {
        CleanList(spawnedShirtSlots);
        CleanList(spawnedPantSlots);
        CleanList(spawnedShoesSlots);
        CleanList(allSkinSlots);
    }

    /// <summary>
    /// Safely removes destroyed/null entries from a list.
    /// </summary>
    private void CleanList(List<SkinSlot> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null)
                list.RemoveAt(i);
        }
    }

    // ══════════════════════════════════════════════════════
    // 🔍 Utility
    // ══════════════════════════════════════════════════════
    public SkinDataSO GetSkinDataByID(string skinID)
    {
        if (string.IsNullOrEmpty(skinID))
            return null;

        return allSkins.FirstOrDefault(s => s.name == skinID);
    }

    // ══════════════════════════════════════════════════════
    // 🌀 Preview Rotation Control
    // ══════════════════════════════════════════════════════
    /// <summary>
    /// Called by Reset button to restore model to its original rotation.
    /// </summary>
    private void ResetModelRotation()
    {
        if (previewRotator != null)
            previewRotator.ResetRotation();

        else Debug.LogWarning("[OutfitManager] No CharacterPreviewRotator assigned!");

        AudioManager.Instance.PlayInteractSound(8);
    }

    // ══════════════════════════════════════════════════════
    // 🧥 Default Skin Reset
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Resets all equipped outfits of the current character to their default skins.
    /// </summary>
    private void ResetToDefaultSkins()
    {
        if (currentCharacter == CharacterType.All)
        {
            Debug.LogWarning("[OutfitManager] Cannot reset when CharacterType = All.");
            return;
        }

        // Find all default skins that belong to the current character
        var defaultSkins = allSkins
            .Where(s => s.owner == currentCharacter && s.isDefaultSkin)
            .ToList();

        if (defaultSkins.Count == 0)
        {
            Debug.Log($"[OutfitManager] No default skins found for {currentCharacter}.");
            return;
        }

        // Unequip all current outfits
        equippedOutfits.RemoveAll(e => e.owner == currentCharacter);

        // Equip each default skin
        foreach (var skin in defaultSkins)
        {
            equippedOutfits.Add(new EquippedOutfitEntry
            {
                owner = currentCharacter,
                outfitType = skin.skinType,
                skinID = skin.name
            });

            // Ensure it's marked unlocked
            if (!unlockedSkins.Contains(skin.name))
                unlockedSkins.Add(skin.name);
        }

        // Apply changes visually and save
        RefreshEquippedVisuals();
        AutoSave();

        AudioManager.Instance.PlayInteractSound(8);

        Debug.Log($"[OutfitManager] Reset {currentCharacter}'s outfits to default skins.");
    }
}
