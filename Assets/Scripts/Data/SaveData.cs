using System;
using System.Collections.Generic;

[Serializable]
public class InventorySlotData
{
    public IngredientType ingredientType;
    public int quantity;
}

[Serializable]
public class StatSaveData
{
    public StatType statType;
    public int level;
}

[Serializable]
public class PlateSaveData
{
    public ItemType itemType;
    public float remainingTime;
    public long lastBakeTimestamp;
}

[System.Serializable]
public class OwnedDecorationEntry
{
    public int id;
    public CharacterType owner;
}

[System.Serializable]
public class EquippedOutfitEntry
{
    public string skinID;
    public CharacterType owner;
    public OutfitType outfitType;
}

[Serializable]
public class SaveData
{
    // ─────────────────────────────────────────────────────
    // 🟦 Story System
    // ─────────────────────────────────────────────────────
    public bool hasViewedStoryFirstTime;

    // ─────────────────────────────────────────────────────
    // 🧍 Player Core Stats
    // ─────────────────────────────────────────────────────
    public int playerLevel = 1;
    public double playerMoney = 0;
    public double playerDiamond = 0;
    public int statPoints = 0;

    // ─────────────────────────────────────────────────────
    // ❤️ Player Status
    // ─────────────────────────────────────────────────────
    public bool hasStats;
    public float moodCurrent;
    public float energyCurrent;
    public float engagementCurrent;

    public float remainingBedCooldown;

    public int ownedCoins;

    public float totalDecayRate;
    public List<MoodConditionType> moodQueueList = new();

    // ─────────────────────────────────────────────────────
    // 📱 Social / Post System
    // ─────────────────────────────────────────────────────
    public bool hasPostedFirstTime;

    // ─────────────────────────────────────────────────────
    // 🎯 Quest & Bonus
    // ─────────────────────────────────────────────────────
    public string lastQuestDate;
    public bool hasClaimedDailyBonus;
    public List<DailyQuestData> dailyQuests = new();

    // ─────────────────────────────────────────────────────
    // 🪑 Decoration & Inventory
    // ─────────────────────────────────────────────────────
    public List<OwnedDecorationEntry> ownedDecorations = new();
    public List<PlateSaveData> plates = new();
    public List<StatSaveData> statLevels = new();
    public List<InventorySlotData> foodInventory = new();
    public List<InventorySlotData> cakeInventory = new();

    // ─────────────────────────────────────────────────────
    // 👗 Outfit / Skin System
    // ─────────────────────────────────────────────────────
    public List<string> unlockedSkins = new();
    public List<EquippedOutfitEntry> equippedOutfits = new();
}
