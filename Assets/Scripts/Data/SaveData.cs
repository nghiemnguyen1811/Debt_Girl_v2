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

[Serializable]
public class SaveData
{
    public int playerLevel = 1;
    public double playerMoney = 0;
    public int statPoints = 0;

    public bool hasStats;
    public float moodCurrent;
    public float energyCurrent;
    public float engagementCurrent;

    public float remainingBedCooldown;

    public int ownedCoins;

    public float totalDecayRate;
    public List<MoodConditionType> moodQueueList = new();

    public bool hasPostedFirstTime;

    public string lastQuestDate;
    public bool hasClaimedDailyBonus;
    public List<DailyQuestData> dailyQuests = new();

    public List<OwnedDecorationEntry> ownedDecorations = new();
    public List<PlateSaveData> plates = new();
    public List<StatSaveData> statLevels = new();
    public List<InventorySlotData> foodInventory = new();
    public List<InventorySlotData> cakeInventory = new();
}
