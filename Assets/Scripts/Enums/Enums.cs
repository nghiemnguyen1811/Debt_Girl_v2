public enum FloorType
{
    None,
    Yard,
    F1,
    F2,
    F3,
}

public enum RoomType
{
    None,
    WareHouse,
    HongYeoSa,
    LivingRoom,
    WC_F1,
    Jiho,
    Danbi,
    WC_F2,
    ChoHee,
    SonsRoom,
    TaeSeon,
    ExcitedRoom,
    YoonSeul
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
    Yawn,
    Hungry,
    Dirty,
    Sleep,
    Clean,
    Typing,
    Cooking,
    DirtyTeeth,
    BrushTeeth,
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
    Almond,
    BakingPowder,
    BakingSoda,
    Banana,
    Butter,
    ChocolateBar,
    ChocolateChips,
    Chocolate,
    CoconutMilk,
    Coffee,
    CondensedMilk,
    CreamCheese,
    Egg,
    FreshMilk,
    FriedEgg,
    Gelatin,
    GlutinouseRiceFlour,
    Honey,
    Ladyfinger,
    MatchaPowder,
    Mayonnaise,
    Milk,
    PineappleJam,
    PorkFloss,
    RedBeanPaste,
    SaltedEgg,
    Strawberry,
    Sugar,
    VanillaExtract,
    Walnut,
    WhippedCream,
    WhippingCream,

    // ───── Consumable ─────
    Pizza,
    DrinkCup,

    // ───── CraftedFood Dish ─────
    NoodleSoup,
    GrilledMeat,

    // ───── CraftedFood Desserts ─────
    BananaBread,
    Brownie,
    ButterCookies,
    Cheesecake,
    ChocolateChipCookies,
    Crepe,
    Cupcake,
    Flan,
    MatchaRollCake,
    Mochi,
    RedVelvetCake,
    SaltedEggSpongeCake,
    SpongeCake,
    StrawberryMousseCake,
    Tiramisu,

    Meat
}

public enum InteractionPropType
{
    None,
    Broom,
    CookingPan,
    Toothbrush,
}