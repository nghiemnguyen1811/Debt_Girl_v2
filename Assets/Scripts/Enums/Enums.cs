public enum CharacterType
{
    All,
    Danbi,
    TaeSeon,
    Jiho,
    YoonSeul,
    ChoHee,
}

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
    YoonSeul,
    Rooftop
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
    Shower,
    Sleep,
    Clean,
    Typing,
    Cooking,
    DirtyTeeth,
    BrushTeeth,
    DirtyBody,
    Exercise
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

    #region ───── Material: Desserts ─────
    Almond, BakingPowder, BakingSoda,
    Banana, Butter, ChocolateBar,
    ChocolateChips, Chocolate, CoconutMilk,
    Coffee, CondensedMilk, CreamCheese,
    Egg, FreshMilk, FriedEgg,
    Gelatin, GlutinouseRiceFlour, Honey,
    Ladyfinger, MatchaPowder, Mayonnaise,
    Milk, PineappleJam, PorkFloss,
    RedBeanPaste, SaltedEgg, Strawberry,
    Sugar, VanillaExtract, Walnut,
    WhippedCream, WhippingCream,
    #endregion

    #region ───── CraftedFood: Desserts ─────
    BananaBread, Brownie, ButterCookies,
    Cheesecake, ChocolateChipCookies, Crepe,
    Cupcake, Flan, MatchaRollCake,
    Mochi, RedVelvetCake, SaltedEggSpongeCake,
    SpongeCake, StrawberryMousseCake, Tiramisu,
    #endregion

    #region ───── Material: Dishes ─────
    Rice, Cabbage, Chili,
    Garlic, Carrot, Onion,
    RiceCake, FishCake, Pork,
    Tofu, GlassNoodles, Beef,
    Vegetables, PorkBelly, Lettuce,
    Seaweed, Seafood, SoySauce,
    ChiliSauce, Ginseng, Flour,
    GreenOnion, PorkIntestine, BlackBeanPaste,
    BeefRibs, Radish, PorkBones,
    ColdNoodles, Cucumber, Crab,
    Chicken, Potato,
    #endregion

    #region ───── CraftedFood: Dishes ─────
    SteamedRice, Kimchi, GyeranMari,
    Tteokbokki, KimchiJjigae, Japchae,
    Mandu, Bibimbap, Samgyeopsal,
    Gimbap, SundubuJjigae, Bulgogi,
    KimchiFriedRice, Odeng, Yukgaejang,
    Samgyetang, HaemulPajeon, Soondae,
    Jajangmyeon, Dakgangjeong, Galbitang,
    KimchiJeon, EomukBokkeum, Jjajangbap,
    Bossam, Gamjatang, Haejangguk,
    BibimNaengmyeon, Galbi, Jjamppong,
    #endregion
}

public enum InteractionPropType
{
    None,
    Broom,
    CookingPan,
    Toothbrush,
    Dumbbell,
    Towel,
    Phone
}

public enum PanelType
{
    Phone,
    Post,
    Exit,
    Upgrade,
    CoinTrade,
    Shopping,
    FoodInventory,
    CakeInventory,
    Baking,
    Cooking,
    SelectRoom,
    Dialogue,
    Settings,
    Pause,
    Banking
}

public enum AppType
{
    Sns,
    Baking,
    Cooking,
    TradingCoin
}