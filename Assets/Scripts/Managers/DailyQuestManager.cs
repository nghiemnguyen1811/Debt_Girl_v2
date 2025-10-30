using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using URandom = UnityEngine.Random;

// ==================================================
// ▶ QUEST UI GROUP STRUCT
// ==================================================
[Serializable]
public class QuestUIGroup
{
    [Header("Quest UI Elements")]
    public GameObject questGroup;
    public TextMeshProUGUI descriptionText;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public GameObject completedMark;

    [Header("Reward Buttons")]
    public Button rewardButtonEnable;
    public Button rewardButtonDisable;
    public Button claimedButton;
}

// ==================================================
// ▶ DAILY QUEST MANAGER
// ==================================================
public class DailyQuestManager : SingletonMonobehaviour<DailyQuestManager>
{
    // ─────────────────────────────────────────────────────
    // INSPECTOR FIELDS
    // ─────────────────────────────────────────────────────
    [Header("Quest Database")]
    [SerializeField] private List<DailyQuestDataSO> questTemplates = new();
    [SerializeField] private int dailyQuestCount = 3;

    [Header("Runtime Data")]
    [SerializeField] private List<DailyQuestData> activeQuests = new();

    [Header("UI References")]
    [SerializeField] private QuestUIGroup[] questUIGroups;
    [SerializeField] private GameObject[] separatorLines;

    [Header("Completion Reward UI")]
    [SerializeField] private Button completionRewardEnable;
    [SerializeField] private Button completionRewardDisable;
    [SerializeField] private Button completionRewardClaimed;
    [SerializeField] private TextMeshProUGUI[] bonusDiamondTexts;

    [Header("Bonus Reward Settings")]
    [SerializeField] private int bonusDiamondAmount = 1;

    [Header("Registered Interactables")]
    [SerializeField] private List<InteractableBase> registeredInteractables = new();

    // ─────────────────────────────────────────────────────
    // PRIVATE FIELDS
    // ─────────────────────────────────────────────────────
    private string currentDate;
    private float checkInterval = 60f;
    private bool hasClaimedBonus;

    // Cached event handlers
    private Action cakeBakedHandler, dishCookedHandler, debtPaidHandler;
    private Action coinBoughtHandler, coinSellHandler, postCreatedHandler, itemPurchasedHandler;

    // ─────────────────────────────────────────────────────
    // PROPERTIES
    // ─────────────────────────────────────────────────────
    /// <summary>Returns the player's current level (default = 1 if GameManager not initialized).</summary>
    private int PlayerLevel => GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;

    // ==================================================
    // ▶ UNITY LIFECYCLE
    // ==================================================
    private void Start() => InitializeSystem();

    private void OnDestroy()
    {
        UnsubscribeEvents();
        StopAllCoroutines();
    }

    // ==================================================
    // ▶ INITIALIZATION
    // ==================================================
    /// <summary>Initializes the system, sets up UI, listeners, and date check routine.</summary>
    private void InitializeSystem()
    {
        currentDate = DateTime.Now.ToString("yyyy-MM-dd");

        SubscribeToEvents();
        InitializeBonusUI();
        SetupListeners();
        RefreshUI();
        StartCoroutine(CheckDateChangeRoutine());
    }

    /// <summary>Sets up the bonus button listener.</summary>
    private void SetupListeners()
    {
        if (completionRewardEnable == null) return;

        completionRewardEnable.onClick.RemoveAllListeners();
        completionRewardEnable.onClick.AddListener(ClaimDailyBonus);
    }

    /// <summary>Displays the diamond bonus amount in UI.</summary>
    private void InitializeBonusUI()
    {
        foreach (TextMeshProUGUI bonusText in bonusDiamondTexts)
            bonusText.text = $"{bonusDiamondAmount}";
    }

    // ==================================================
    // ▶ BONUS CLAIMING
    // ==================================================
    /// <summary>Handles claiming the daily diamond bonus after all quests are completed.</summary>
    private void ClaimDailyBonus()
    {
        if (hasClaimedBonus)
        {
            Debug.Log("[DailyQuestManager] Bonus already claimed for today.");
            return;
        }

        hasClaimedBonus = true;
        MoneyManager.Instance.ChangeDiamonds(bonusDiamondAmount);
        AudioManager.Instance.PlayInteractSound(14);

        AutoSave();
        UpdateCompletionRewardUI();
    }

    // ==================================================
    // ▶ INTERACTABLE REGISTRATION
    // ==================================================
    /// <summary>Registers an InteractableBase and subscribes its OnStopInteractable event.</summary>
    public void RegisterInteractable(InteractableBase interactable)
    {
        if (!registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Add(interactable);
            interactable.OnStopInteractable += AddProgress;
        }
    }

    /// <summary>Unregisters an InteractableBase and unsubscribes its event.</summary>
    public void UnregisterInteractable(InteractableBase interactable)
    {
        if (registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Remove(interactable);
            interactable.OnStopInteractable -= AddProgress;
        }
    }

    // ==================================================
    // ▶ QUEST GENERATION
    // ==================================================
    private void GenerateNewDailyQuests()
    {
        activeQuests.Clear();

        List<DailyQuestDataSO> availableQuests = GetAvailableQuestsForLevel(PlayerLevel);
        if (availableQuests.Count == 0)
        {
            Debug.LogWarning($"[DailyQuestManager] No quests available for level {PlayerLevel}.");
            return;
        }

        List<DailyQuestDataSO> shuffled = ShuffleList(availableQuests);
        CreateActiveQuestsFromTemplates(shuffled, PlayerLevel);
        AutoSave();

        Debug.Log($"[DailyQuestManager] Created {activeQuests.Count} quests for player level {PlayerLevel}.");
    }

    /// <summary>Returns quests available for the given player level.</summary>
    private List<DailyQuestDataSO> GetAvailableQuestsForLevel(int playerLevel)
        => questTemplates.FindAll(q => q.requiredLevel <= playerLevel);

    /// <summary>Randomly shuffles a list using UnityEngine.Random.</summary>
    private List<DailyQuestDataSO> ShuffleList(List<DailyQuestDataSO> source)
    {
        List<DailyQuestDataSO> shuffled = new(source);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int randomIndex = URandom.Range(i, shuffled.Count);
            (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
        }
        return shuffled;
    }

    /// <summary>Creates daily quest instances from the shuffled template list.</summary>
    private void CreateActiveQuestsFromTemplates(List<DailyQuestDataSO> shuffled, int playerLevel)
    {
        int questToAdd = Mathf.Min(dailyQuestCount, shuffled.Count);

        for (int i = 0; i < questToAdd; i++)
        {
            var template = shuffled[i];

            if (template.questType == DailyQuestType.Interact &&
                template.activityRequirements != null &&
                template.activityRequirements.Length > 0)
            {
                List<DailyActivityRequirement> validActivities = new();
                foreach (var req in template.activityRequirements)
                    if (req.requiredLevel <= playerLevel)
                        validActivities.Add(req);

                if (validActivities.Count == 0)
                {
                    Debug.Log($"[DailyQuestManager] Player level {playerLevel} too low for Interact quest.");
                    continue;
                }

                var chosenActivity = validActivities[URandom.Range(0, validActivities.Count)];
                activeQuests.Add(new DailyQuestData
                {
                    questID = template.name,
                    questTemplate = template,
                    targetCount = URandom.Range(template.minTarget, template.maxTarget + 1),
                    currentCount = 0,
                    isCompleted = false,
                    selectedActivity = chosenActivity.activity,
                    savedActivityInt = (int)chosenActivity.activity
                });

                Debug.Log($"[DailyQuestManager] Interact quest → chose activity: {chosenActivity.activity}");
                continue;
            }

            // Normal quest
            activeQuests.Add(new DailyQuestData
            {
                questID = template.name,
                questTemplate = template,
                targetCount = URandom.Range(template.minTarget, template.maxTarget + 1),
                currentCount = 0,
                isCompleted = false
            });
        }
    }

    // ==================================================
    // ▶ QUEST PROGRESSION
    // ==================================================
    public void AddProgress(DailyQuestType type, DailyActivity activity = DailyActivity.None)
    {
        bool changed = false;

        foreach (var quest in activeQuests)
        {
            if (!CanProgressQuest(quest, type, activity, PlayerLevel))
                continue;

            Debug.Log(CanProgressQuest(quest, type, activity, PlayerLevel));

            int before = quest.currentCount;
            quest.AddProgress();

            if (quest.currentCount != before) changed = true;
        }

        if (changed)
        {
            RefreshUI();
            AutoSave();
        }
    }

    /// <summary>Checks if the given quest can progress based on quest type and activity.</summary>
    private bool CanProgressQuest(DailyQuestData quest, DailyQuestType type, DailyActivity activity, int playerLevel)
    {
        if (quest == null || quest.isCompleted || quest.questTemplate == null) return false;
        if (quest.questTemplate.requiredLevel > playerLevel) return false;

        if (quest.questTemplate.questType != DailyQuestType.Interact)
            return quest.questTemplate.questType == type;

        if (type != DailyQuestType.Interact || quest.selectedActivity == DailyActivity.None)
            return false;

        return quest.selectedActivity == activity;
    }

    // ==================================================
    // ▶ UI HANDLING
    // ==================================================
    private void RefreshUI()
    {
        if (questUIGroups == null || questUIGroups.Length == 0)
        {
            Debug.LogWarning("[DailyQuestManager] No Quest UI Groups assigned.");
            return;
        }

        int questCount = activeQuests.Count;

        for (int i = 0; i < questUIGroups.Length; i++)
        {
            var ui = questUIGroups[i];
            bool active = i < questCount;
            ui.questGroup?.SetActive(active);
            if (!active) continue;

            var quest = activeQuests[i];
            ui.descriptionText.text = quest.Description;
            ui.progressText.text = $"{quest.currentCount}/{quest.targetCount}";
            ui.rewardText.text = $"x {quest.questTemplate.rewardDiamond}";
            ui.progressBar.value = (float)quest.currentCount / quest.targetCount;
            ui.completedMark.SetActive(quest.isCompleted);

            // ✅ Update reward button visibility
            bool canClaim = quest.isCompleted && !quest.hasClaimedReward;
            bool alreadyClaimed = quest.isCompleted && quest.hasClaimedReward;

            ui.rewardButtonEnable.gameObject.SetActive(canClaim);
            ui.rewardButtonDisable.gameObject.SetActive(!quest.isCompleted);
            ui.claimedButton.gameObject.SetActive(alreadyClaimed);

            // ✅ Set listener for claim button
            ui.rewardButtonEnable.onClick.RemoveAllListeners();
            int index = i;
            ui.rewardButtonEnable.onClick.AddListener(() => ClaimQuestReward(index));
        }

        for (int i = 0; i < separatorLines.Length; i++)
            separatorLines[i]?.SetActive(i < questCount - 1);

        UpdateCompletionRewardUI();
    }

    /// <summary>
    /// Called when player clicks reward button to claim quest reward.
    /// </summary>
    public void ClaimQuestReward(int questIndex)
    {
        if (questIndex < 0 || questIndex >= activeQuests.Count) return;
        var quest = activeQuests[questIndex];

        if (!quest.isCompleted || quest.hasClaimedReward)
            return;

        quest.hasClaimedReward = true;
        MoneyManager.Instance.ChangeDiamonds(quest.questTemplate.rewardDiamond);
        AudioManager.Instance.PlayInteractSound(14);

        // ✅ Update UI state
        var ui = questUIGroups[questIndex];
        ui.rewardButtonEnable.gameObject.SetActive(false);
        ui.rewardButtonDisable.gameObject.SetActive(false);
        ui.claimedButton.gameObject.SetActive(true);

        AutoSave();
    }

    /// <summary>Updates the bonus claim buttons based on completion state.</summary>
    private void UpdateCompletionRewardUI()
    {
        bool allCompleted = activeQuests.TrueForAll(q => q.isCompleted);

        completionRewardEnable?.gameObject.SetActive(allCompleted && !hasClaimedBonus);
        completionRewardDisable?.gameObject.SetActive(!allCompleted);
        completionRewardClaimed?.gameObject.SetActive(allCompleted && hasClaimedBonus);
    }

    // ==================================================
    // ▶ SAVE & LOAD
    // ==================================================
    public void ImportSaveData(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[DailyQuestManager] ImportSaveData() received null SaveData.");
            return;
        }

        activeQuests = data.dailyQuests ?? new List<DailyQuestData>();
        currentDate = data.lastQuestDate ?? DateTime.Now.ToString("yyyy-MM-dd");
        hasClaimedBonus = data.hasClaimedDailyBonus;

        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (string.IsNullOrEmpty(data.lastQuestDate) || data.lastQuestDate != today)
        {
            string logText = string.IsNullOrEmpty(data.lastQuestDate)
                ? "[DailyQuestManager] First-time login → generating daily quests."
                : $"[DailyQuestManager] New day detected ({data.lastQuestDate} → {today}) → regenerating quests.";

            Debug.Log(logText);
            hasClaimedBonus = false;
            currentDate = today;
            GenerateNewDailyQuests();
            AutoSave(true);
        }

        else Debug.Log($"[DailyQuestManager] Same day ({today}) → keeping previous quests.");

        foreach (var quest in activeQuests)
        {
            if (quest.questTemplate == null && !string.IsNullOrEmpty(quest.questID))
                quest.questTemplate = questTemplates.Find(q => q.name == quest.questID);

            quest.selectedActivity = (DailyActivity)quest.savedActivityInt;
        }

        RefreshUI();
    }

    public void AutoSave(bool includeDate = false)
    {
        if (SaveManager.Data == null) return;

        SaveManager.Data.dailyQuests = activeQuests;
        SaveManager.Data.hasClaimedDailyBonus = hasClaimedBonus;

        if (includeDate)
            SaveManager.Data.lastQuestDate = currentDate;

        SaveManager.SaveGame();
    }

    public void ResetDailyQuests()
    {
        GenerateNewDailyQuests();
        AutoSave(true);
        RefreshUI();
    }

    // ==================================================
    // ▶ EVENT SUBSCRIPTIONS
    // ==================================================
    private void SubscribeToEvents() => HandleEventSubscriptions(true);
    private void UnsubscribeEvents() => HandleEventSubscriptions(false);

    /// <summary>Subscribes or unsubscribes quest-related events from all managers.</summary>
    private void HandleEventSubscriptions(bool subscribe)
    {
        // Baking
        if (BakingManager.Instance != null)
        {
            if (subscribe)
            {
                cakeBakedHandler = () => AddProgress(DailyQuestType.BakeCake);
                BakingManager.Instance.OnCakeBaked += cakeBakedHandler;
            }
            else BakingManager.Instance.OnCakeBaked -= cakeBakedHandler;
        }

        // Cooking
        if (CookingManager.Instance != null)
        {
            if (subscribe)
            {
                dishCookedHandler = () => AddProgress(DailyQuestType.Cooking);
                CookingManager.Instance.OnDishCooked += dishCookedHandler;
            }
            else CookingManager.Instance.OnDishCooked -= dishCookedHandler;
        }

        // Banking
        if (BankManager.Instance != null)
        {
            if (subscribe)
            {
                debtPaidHandler = () => AddProgress(DailyQuestType.PayDebt);
                BankManager.Instance.OnDebtPaid += debtPaidHandler;
            }
            else BankManager.Instance.OnDebtPaid -= debtPaidHandler;
        }

        // Coin Trading
        if (CoinTradeManager.Instance != null)
        {
            if (subscribe)
            {
                coinBoughtHandler = () => AddProgress(DailyQuestType.BuyCoin);
                coinSellHandler = () => AddProgress(DailyQuestType.SellCoin);
                CoinTradeManager.Instance.OnCoinBought += coinBoughtHandler;
                CoinTradeManager.Instance.OnCoinSell += coinSellHandler;
            }
            else
            {
                CoinTradeManager.Instance.OnCoinBought -= coinBoughtHandler;
                CoinTradeManager.Instance.OnCoinSell -= coinSellHandler;
            }
        }

        // Posts
        if (PostManager.Instance != null)
        {
            if (subscribe)
            {
                postCreatedHandler = () => AddProgress(DailyQuestType.CreatePosts);
                PostManager.Instance.OnPostCreated += postCreatedHandler;
            }
            else PostManager.Instance.OnPostCreated -= postCreatedHandler;
        }

        // Shop
        if (ShopManager.Instance != null)
        {
            if (subscribe)
            {
                itemPurchasedHandler = () => AddProgress(DailyQuestType.BuyFromShop);
                ShopManager.Instance.OnItemPurchased += itemPurchasedHandler;
            }
            else ShopManager.Instance.OnItemPurchased -= itemPurchasedHandler;
        }

        Debug.Log($"[DailyQuestManager] {(subscribe ? "Subscribed" : "Unsubscribed")} to events.");
    }

    // ==================================================
    // ▶ COROUTINES
    // ==================================================
    private IEnumerator CheckDateChangeRoutine()
    {
        while (true)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (today != currentDate)
            {
                currentDate = today;
                GenerateNewDailyQuests();
                AutoSave(true);
                RefreshUI();
                Debug.Log($"[DailyQuestManager] Day changed → new daily quests generated for {today}");
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
}
