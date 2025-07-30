public enum RoomType
{
    None,
    LivingRoom,
    Kitchen,
    Bedroom,
    Bathroom,
    Office,
    Laboratory,
    Storage,
    Hallway
}

public enum StatType
{
    IncomeRate,
    Productivity,
    Mood
}

public enum MoodConditionType
{
    None,
    Normal,
    Hungry,
    Dirty,
    Sleepy,
    NeedToShower,
    Bored,
    Stressed
}

public enum AudioGroup
{
    Music,
    Sound,
    Footstep,
    Mood
}

public enum EngagementLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

public enum ItemType
{
    Material,
    CraftedFood,
    Consumable,
    Equipment,
    QuestItem
}

public enum InteractionPlayMode
{
    Instant,         // Âm thanh + animation ngay khi interact
    WaitForConfirm,  // Chờ nhấn nút confirm sau UI
    SoundOnly        // Chỉ phát âm thanh, không animation
}

public enum IngredientType
{
    None,

    // ───── Material ─────
    Meat,
    Fish,
    Butter,
    Peanut,
    Flour,
    Sugar,
    Egg,
    Milk,
    InstantNoodles,
    SpicyNoodles,
    BottleWater,

    // ───── Consumable ─────
    Pizza,
    DrinkCup,

    // ───── CraftedFood Dish ─────
    NoodleSoup,
    GrilledMeat,

    // ───── CraftedFood Desserts ─────
    SpongeCake,
    EggCake,
}