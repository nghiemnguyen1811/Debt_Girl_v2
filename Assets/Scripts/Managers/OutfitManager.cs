using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OutfitManager : SingletonMonobehaviour<OutfitManager>
{
    // ─────────────────────────────────────────────────────
    // 🧩 Inspector Fields
    // ─────────────────────────────────────────────────────
    [Header("Skin Slot References")]
    [SerializeField] private Transform hatParent;
    [SerializeField] private Transform topParent;
    [SerializeField] private Transform shoesParent;
    [SerializeField] private SkinSlot skinSlotPrefab;

    [Header("All Skin Data")]
    [SerializeField] private List<SkinDataSO> allSkins = new();

    [Header("Character Tabs")]
    [SerializeField] private Transform characterTabParent;
    [SerializeField] private CharacterTabButton characterTabPrefab;
    [SerializeField] private List<CharacterInfoSO> characterTabList;

    [Header("Outfit Tabs")]
    [SerializeField] private List<Tab> outfitTabs; // 3 tabs: Hat / Top / Shoes

    [Header("Equip Button")]
    [SerializeField] private Button equipButtonEnabled;
    [SerializeField] private Button equipButtonDisabled;

    // ─────────────────────────────────────────────────────
    // 🧠 Runtime Data
    // ─────────────────────────────────────────────────────
    private readonly List<CharacterTabButton> spawnedCharacterTabs = new();
    private readonly List<SkinSlot> spawnedHatSlots = new();
    private readonly List<SkinSlot> spawnedTopSlots = new();
    private readonly List<SkinSlot> spawnedShoesSlots = new();
    private readonly List<SkinSlot> allSkinSlots = new();

    private CharacterTabButton currentSelectedTab;
    private Tab currentActiveTab;
    private CharacterType currentCharacter;
    private SkinSlot currentSelectedSlot;

    // ─────────────────────────────────────────────────────
    // 💾 Save Data Cache
    // ─────────────────────────────────────────────────────
    private List<string> ownedSkins = new();
    private List<EquippedOutfitEntry> equippedOutfits = new();

    // ─────────────────────────────────────────────────────
    // 🏁 Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void OnEnable()
    {
        if (PlayerControl.Instance != null)
            PlayerControl.Instance.OnCharacterProfileChanged += HandleCharacterChanged;

        if (equipButtonEnabled != null)
            equipButtonEnabled.onClick.AddListener(OnEquipButtonClicked);
    }

    private void OnDisable()
    {
        if (PlayerControl.Instance != null)
            PlayerControl.Instance.OnCharacterProfileChanged -= HandleCharacterChanged;

        if (equipButtonEnabled != null)
            equipButtonEnabled.onClick.RemoveListener(OnEquipButtonClicked);
    }

    private void Start()
    {
        InitializeCharacterTabs();
        InitializeOutfitUI();
        SetupTabs();

        CharacterTabButton.OnTabSelected += HandleCharacterTabSelected;

        if (outfitTabs.Count > 0)
            ActivateTab(outfitTabs[1]);

        if (PlayerControl.Instance != null && PlayerControl.Instance.CharacterProfile != null)
            currentCharacter = PlayerControl.Instance.CharacterProfile.characterType;

        RefreshEquippedVisuals();
        UpdateEquipButtonState(false);
    }

    private void OnDestroy()
    {
        CharacterTabButton.OnTabSelected -= HandleCharacterTabSelected;
    }

    // ─────────────────────────────────────────────────────
    // 👤 Character Tabs (Switching Characters)
    // ─────────────────────────────────────────────────────
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

    private void HandleCharacterTabSelected(CharacterType selectedType)
    {
        if (currentSelectedTab != null && currentSelectedTab.CharacterType == selectedType)
        {
            currentSelectedTab.SetSelected(false);
            currentSelectedTab = null;
            ShowAllOutfits();
            return;
        }

        if (currentSelectedTab != null)
            currentSelectedTab.SetSelected(false);

        currentSelectedTab = spawnedCharacterTabs.Find(t => t.CharacterType == selectedType);

        if (currentSelectedTab != null)
        {
            currentSelectedTab.SetSelected(true);
            SetCurrentCharacter(selectedType);
        }
    }

    private void HandleCharacterChanged(CharacterInfoSO newProfile)
    {
        if (newProfile == null)
            return;

        // Update current character
        currentCharacter = newProfile.characterType;

        // Refresh UI for new character
        FilterByCharacter(currentCharacter);
        RefreshEquippedVisuals();

        // Check if current selected slot matches new character
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

        // Update Equip button state
        UpdateEquipButtonState(canEquip);

        Debug.Log($"👤 Character switched to: {currentCharacter}. Refreshed equipped outfits.");
    }

    private void ShowAllOutfits()
    {
        foreach (var slot in allSkinSlots)
            slot.gameObject.SetActive(true);
    }

    private void SetCurrentCharacter(CharacterType character)
    {
        currentCharacter = character;
        FilterByCharacter(character);
        RefreshEquippedVisuals();
    }

    private void FilterByCharacter(CharacterType type)
    {
        void FilterList(List<SkinSlot> list)
        {
            foreach (var slot in list)
                slot.gameObject.SetActive(slot.SkinData.owner == type);
        }

        FilterList(spawnedHatSlots);
        FilterList(spawnedTopSlots);
        FilterList(spawnedShoesSlots);
    }

    // ─────────────────────────────────────────────────────
    // 🎨 Outfit UI Setup
    // ─────────────────────────────────────────────────────
    private void InitializeOutfitUI()
    {
        var hatList = allSkins.Where(s => s.skinType == OutfitType.Hat).ToList();
        var topList = allSkins.Where(s => s.skinType == OutfitType.Top).ToList();
        var shoesList = allSkins.Where(s => s.skinType == OutfitType.Shoes).ToList();

        SpawnSlots(hatList, hatParent, spawnedHatSlots);
        SpawnSlots(topList, topParent, spawnedTopSlots);
        SpawnSlots(shoesList, shoesParent, spawnedShoesSlots);
    }

    private void SpawnSlots(List<SkinDataSO> dataList, Transform parent, List<SkinSlot> spawnedList)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);

        spawnedList.Clear();

        foreach (var data in dataList)
        {
            var slot = Instantiate(skinSlotPrefab, parent);
            bool unlocked = ownedSkins.Contains(data.name);
            slot.Setup(data, unlocked);
            spawnedList.Add(slot);
            allSkinSlots.Add(slot);
        }
    }

    // ─────────────────────────────────────────────────────
    // 🧥 Outfit Tabs
    // ─────────────────────────────────────────────────────
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

    private void SetTabVisual(Tab tab, bool isActive)
    {
        if (tab.outline != null)
            tab.outline.SetActive(isActive);
    }

    // ─────────────────────────────────────────────────────
    // 🧩 Outfit Selection & Equip Button
    // ─────────────────────────────────────────────────────
    public void OnSkinSelected(SkinSlot selectedSkin)
    {
        foreach (var slot in allSkinSlots)
        {
            if (!slot.IsEquipped)
                slot.SetSelected(false);
        }

        currentSelectedSlot = selectedSkin;

        bool canEquip = false;
        if (currentSelectedSlot != null && currentSelectedSlot.IsUnlocked)
            canEquip = selectedSkin.SkinData.owner == currentCharacter;

        UpdateEquipButtonState(canEquip);
    }

    private void UpdateEquipButtonState(bool active)
    {
        if (equipButtonEnabled == null || equipButtonDisabled == null) return;

        equipButtonEnabled.gameObject.SetActive(active);
        equipButtonDisabled.gameObject.SetActive(!active);
    }

    private void OnEquipButtonClicked()
    {
        if (currentSelectedSlot == null || !currentSelectedSlot.IsUnlocked)
        {
            Debug.LogWarning("No skin selected or skin not unlocked!");
            return;
        }

        EquipSkin(currentSelectedSlot.SkinData);
    }

    // ─────────────────────────────────────────────────────
    // 👕 Equip / Unequip Logic
    // ─────────────────────────────────────────────────────
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
        Debug.Log($"[{currentCharacter}] equipped {newSkin.skinType}: {newSkin.name}");
    }

    private List<SkinSlot> GetSlotList(OutfitType type)
    {
        return type switch
        {
            OutfitType.Hat => spawnedHatSlots,
            OutfitType.Top => spawnedTopSlots,
            OutfitType.Shoes => spawnedShoesSlots,
            _ => null
        };
    }

    // ─────────────────────────────────────────────────────
    // 💎 Unlock Refresh
    // ─────────────────────────────────────────────────────
    public void RefreshUnlockButtons()
    {
        UpdateList(spawnedHatSlots);
        UpdateList(spawnedTopSlots);
        UpdateList(spawnedShoesSlots);
    }

    private void UpdateList(List<SkinSlot> slots)
    {
        foreach (var slot in slots)
            slot.RefreshUnlockState();
    }

    // ─────────────────────────────────────────────────────
    // 🧩 Refresh Equipped Visuals
    // ─────────────────────────────────────────────────────
    private void RefreshEquippedVisuals()
    {
        if (currentCharacter == CharacterType.All)
            return;

        foreach (var entry in equippedOutfits.Where(e => e.owner == currentCharacter))
        {
            switch (entry.outfitType)
            {
                case OutfitType.Hat: ApplyEquippedVisual(spawnedHatSlots, entry.skinID); break;
                case OutfitType.Top: ApplyEquippedVisual(spawnedTopSlots, entry.skinID); break;
                case OutfitType.Shoes: ApplyEquippedVisual(spawnedShoesSlots, entry.skinID); break;
            }
        }
    }

    private void ApplyEquippedVisual(List<SkinSlot> slots, string skinID)
    {
        foreach (var slot in slots)
            slot.SetEquipped(slot.SkinData.name == skinID);
    }

    // ─────────────────────────────────────────────────────
    // 💾 Save / Load System
    // ─────────────────────────────────────────────────────
    public void ImportSaveData(SaveData data)
    {
        ownedSkins = data.ownedSkins ?? new List<string>();
        equippedOutfits = data.equippedOutfits ?? new List<EquippedOutfitEntry>();

        if (equippedOutfits.Count == 0)
            AutoEquipDefaultFreeSkins();

        foreach (var slot in allSkinSlots)
        {
            bool isUnlocked = ownedSkins.Contains(slot.SkinData.name);
            slot.UpdateLockState(!isUnlocked);
        }

        RefreshEquippedVisuals();
        Debug.Log("[OutfitManager] Save data imported. Skins and equips refreshed.");
    }

    private void AutoSave()
    {
        SaveManager.Data.ownedSkins = new List<string>(ownedSkins);
        SaveManager.Data.equippedOutfits = new List<EquippedOutfitEntry>(equippedOutfits);
    }

    /// <summary>
    /// Automatically equips default/free skins when no save data exists.
    /// </summary>
    private void AutoEquipDefaultFreeSkins()
    {
        var defaultOrFreeSkins = allSkins
            .Where(s => s.isDefaultSkin || s.sellPrice == 0)
            .ToList();

        foreach (var skin in defaultOrFreeSkins)
        {
            if (!ownedSkins.Contains(skin.name))
                ownedSkins.Add(skin.name);

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

    // ─────────────────────────────────────────────────────
    // 🔓 Unlock System
    // ─────────────────────────────────────────────────────
    public void UnlockSkin(SkinDataSO skin)
    {
        if (!ownedSkins.Contains(skin.name))
            ownedSkins.Add(skin.name);

        AutoSave();
    }

    public bool IsSkinUnlocked(SkinDataSO skin)
    {
        return ownedSkins.Contains(skin.name);
    }
}
