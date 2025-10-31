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
    #region === Inspector Fields ===
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

    [Header("Claim All Reward UI")]
    [SerializeField] private Button claimAllEnable;
    [SerializeField] private Button claimAllDisable;

    [Header("Bonus Reward Settings")]
    [SerializeField] private int bonusDiamondAmount = 1;

    [Header("Registered Interactables")]
    [SerializeField] private List<InteractableBase> registeredInteractables = new();
    #endregion

    #region === Private Fields & Properties ===
    private string currentDate;
    private float checkInterval = 60f;
    private bool hasClaimedBonus;

    // Cached handlers
    private Action cakeBakedHandler, dishCookedHandler, debtPaidHandler;
    private Action coinBoughtHandler, coinSellHandler, postCreatedHandler, itemPurchasedHandler;

    /// <summary>Gets player's current level (default = 1).</summary>
    private int PlayerLevel => GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
    #endregion

    #region === Unity Lifecycle ===
    private void Start() => InitializeSystem(); // 🔹 Entry point
    private void OnDestroy()
    {
        UnsubscribeEvents();
        StopAllCoroutines();
    }
    #endregion

    #region === Initialization ===
    /// <summary>Initializes system, UI, listeners, and daily checks.</summary>
    private void InitializeSystem()
    {
        currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        SubscribeToEvents();
        InitializeBonusUI();
        SetupListeners();
        RefreshUI();
        StartCoroutine(CheckDateChangeRoutine());
    }

    /// <summary>Sets button listeners for reward and claim-all.</summary>
    private void SetupListeners()
    {
        if (completionRewardEnable != null)
        {
            completionRewardEnable.onClick.RemoveAllListeners();
            completionRewardEnable.onClick.AddListener(ClaimDailyBonus);
        }

        if (claimAllEnable != null)
        {
            claimAllEnable.onClick.RemoveAllListeners();
            claimAllEnable.onClick.AddListener(ClaimAllRewards);
        }
    }

    /// <summary>Displays diamond bonus in UI.</summary>
    private void InitializeBonusUI()
    {
        foreach (TextMeshProUGUI bonusText in bonusDiamondTexts)
            bonusText.text = $"{bonusDiamondAmount}";
    }
    #endregion

    #region === Bonus Reward ===
    /// <summary>Claims daily bonus reward when all quests complete.</summary>
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
    #endregion

    #region === Interactable Registration ===
    /// <summary>Registers an interactable quest event.</summary>
    public void RegisterInteractable(InteractableBase interactable)
    {
        if (!registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Add(interactable);
            interactable.OnStopInteractable += AddProgress;
        }
    }

    /// <summary>Unregisters an interactable quest event.</summary>
    public void UnregisterInteractable(InteractableBase interactable)
    {
        if (registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Remove(interactable);
            interactable.OnStopInteractable -= AddProgress;
        }
    }
    #endregion

    #region === Quest Generation ===
    /// <summary>Generates a new set of daily quests.</summary>
    private void GenerateNewDailyQuests()
    {
        activeQuests.Clear();

        var available = GetAvailableQuestsForLevel(PlayerLevel);
        if (available.Count == 0)
        {
            Debug.LogWarning($"[DailyQuestManager] No quests available for level {PlayerLevel}.");
            return;
        }

        var shuffled = ShuffleList(available);
        CreateActiveQuestsFromTemplates(shuffled, PlayerLevel);
        AutoSave();
    }

    /// <summary>Returns all quests available for current level.</summary>
    private List<DailyQuestDataSO> GetAvailableQuestsForLevel(int level)
        => questTemplates.FindAll(q => q.requiredLevel <= level);

    /// <summary>Randomly shuffles quest list.</summary>
    private List<DailyQuestDataSO> ShuffleList(List<DailyQuestDataSO> source)
    {
        List<DailyQuestDataSO> shuffled = new(source);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int r = URandom.Range(i, shuffled.Count);
            (shuffled[i], shuffled[r]) = (shuffled[r], shuffled[i]);
        }
        return shuffled;
    }

    /// <summary>Creates quest instances from templates.</summary>
    private void CreateActiveQuestsFromTemplates(List<DailyQuestDataSO> shuffled, int level)
    {
        int questToAdd = Mathf.Min(dailyQuestCount, shuffled.Count);

        for (int i = 0; i < questToAdd; i++)
        {
            var template = shuffled[i];

            if (template.questType == DailyQuestType.Interact &&
                template.activityRequirements != null &&
                template.activityRequirements.Length > 0)
            {
                List<DailyActivityRequirement> valid = new();
                foreach (var req in template.activityRequirements)
                    if (req.requiredLevel <= level) valid.Add(req);

                if (valid.Count == 0) continue;

                var chosen = valid[URandom.Range(0, valid.Count)];
                activeQuests.Add(new DailyQuestData
                {
                    questID = template.name,
                    questTemplate = template,
                    targetCount = URandom.Range(template.minTarget, template.maxTarget + 1),
                    currentCount = 0,
                    isCompleted = false,
                    selectedActivity = chosen.activity,
                    savedActivityInt = (int)chosen.activity
                });
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
    #endregion

    #region === Quest Progression ===
    /// <summary>Updates quest progress when player performs an action.</summary>
    public void AddProgress(DailyQuestType type, DailyActivity activity = DailyActivity.None)
    {
        bool changed = false;
        foreach (var quest in activeQuests)
        {
            if (!CanProgressQuest(quest, type, activity, PlayerLevel))
                continue;

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

    /// <summary>Checks if quest can be progressed based on activity type.</summary>
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
    #endregion

    #region === UI Handling ===
    /// <summary>Refreshes all quest UI elements and reward states.</summary>
    private void RefreshUI()
    {
        if (questUIGroups == null || questUIGroups.Length == 0) return;
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

            bool canClaim = quest.isCompleted && !quest.hasClaimedReward;
            bool alreadyClaimed = quest.isCompleted && quest.hasClaimedReward;
            ui.rewardButtonEnable.gameObject.SetActive(canClaim);
            ui.rewardButtonDisable.gameObject.SetActive(!quest.isCompleted);
            ui.claimedButton.gameObject.SetActive(alreadyClaimed);

            ui.rewardButtonEnable.onClick.RemoveAllListeners();
            int index = i;
            ui.rewardButtonEnable.onClick.AddListener(() => ClaimQuestReward(index));
        }

        // Handle Claim All button
        int claimableCount = 0;
        foreach (var quest in activeQuests)
            if (quest.isCompleted && !quest.hasClaimedReward) claimableCount++;

        bool canClaimAll = claimableCount >= 2;
        claimAllEnable?.gameObject.SetActive(canClaimAll);
        claimAllDisable?.gameObject.SetActive(!canClaimAll);

        for (int i = 0; i < separatorLines.Length; i++)
            separatorLines[i]?.SetActive(i < questCount - 1);

        UpdateCompletionRewardUI();
    }

    /// <summary>Updates the completion reward (daily bonus) buttons.</summary>
    private void UpdateCompletionRewardUI()
    {
        bool allCompleted = activeQuests.TrueForAll(q => q.isCompleted);

        completionRewardEnable?.gameObject.SetActive(allCompleted && !hasClaimedBonus);
        completionRewardDisable?.gameObject.SetActive(!allCompleted);
        completionRewardClaimed?.gameObject.SetActive(allCompleted && hasClaimedBonus);
    }
    #endregion

    #region === Rewards (Individual & All) ===
    /// <summary>Claims reward for a single quest.</summary>
    public void ClaimQuestReward(int questIndex)
    {
        if (questIndex < 0 || questIndex >= activeQuests.Count) return;
        var quest = activeQuests[questIndex];
        if (!quest.isCompleted || quest.hasClaimedReward) return;

        quest.hasClaimedReward = true;
        MoneyManager.Instance.ChangeDiamonds(quest.questTemplate.rewardDiamond);
        AudioManager.Instance.PlayInteractSound(14);

        var ui = questUIGroups[questIndex];
        ui.rewardButtonEnable.gameObject.SetActive(false);
        ui.rewardButtonDisable.gameObject.SetActive(false);
        ui.claimedButton.gameObject.SetActive(true);
        AutoSave();
    }

    /// <summary>Claims all available quest rewards at once.</summary>
    private void ClaimAllRewards()
    {
        int claimCount = 0;
        foreach (var quest in activeQuests)
        {
            if (quest.isCompleted && !quest.hasClaimedReward)
            {
                quest.hasClaimedReward = true;
                MoneyManager.Instance.ChangeDiamonds(quest.questTemplate.rewardDiamond);
                claimCount++;
            }
        }

        if (claimCount > 0)
        {
            AudioManager.Instance.PlayInteractSound(14);
            AutoSave();
            RefreshUI();
        }
    }
    #endregion

    #region === Save & Load ===
    /// <summary>Imports saved quest data from SaveData.</summary>
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
            hasClaimedBonus = false;
            currentDate = today;
            GenerateNewDailyQuests();
            AutoSave(true);
        }

        foreach (var quest in activeQuests)
        {
            if (quest.questTemplate == null && !string.IsNullOrEmpty(quest.questID))
                quest.questTemplate = questTemplates.Find(q => q.name == quest.questID);

            quest.selectedActivity = (DailyActivity)quest.savedActivityInt;
        }

        RefreshUI();
    }

    /// <summary>Saves quest data to SaveManager.</summary>
    public void AutoSave(bool includeDate = false)
    {
        if (SaveManager.Data == null) return;
        SaveManager.Data.dailyQuests = activeQuests;
        SaveManager.Data.hasClaimedDailyBonus = hasClaimedBonus;
        if (includeDate) SaveManager.Data.lastQuestDate = currentDate;
        SaveManager.SaveGame();
    }

    /// <summary>Resets daily quests manually.</summary>
    public void ResetDailyQuests()
    {
        GenerateNewDailyQuests();
        AutoSave(true);
        RefreshUI();
    }
    #endregion

    #region === Event System ===
    private void SubscribeToEvents() => HandleEventSubscriptions(true);
    private void UnsubscribeEvents() => HandleEventSubscriptions(false);


    /// Subscribes or unsubscribes all quest-related gameplay events.</summary>
    private void HandleEventSubscriptions(bool subscribe)
    {
        // ──────────────────────────────────────────────
        // 🧁 Baking System
        // ──────────────────────────────────────────────
        if (BakingManager.Instance != null)
        {
            if (subscribe)
            {
                cakeBakedHandler = () => AddProgress(DailyQuestType.BakeCake);
                BakingManager.Instance.OnCakeBaked += cakeBakedHandler;
            }
            else BakingManager.Instance.OnCakeBaked -= cakeBakedHandler;
        }

        // ──────────────────────────────────────────────
        // 🍳 Cooking System
        // ──────────────────────────────────────────────
        if (CookingManager.Instance != null)
        {
            if (subscribe)
            {
                dishCookedHandler = () => AddProgress(DailyQuestType.Cooking);
                CookingManager.Instance.OnDishCooked += dishCookedHandler;
            }
            else CookingManager.Instance.OnDishCooked -= dishCookedHandler;
        }

        // ──────────────────────────────────────────────
        // 💰 Bank / Debt System
        // ──────────────────────────────────────────────
        if (BankManager.Instance != null)
        {
            if (subscribe)
            {
                debtPaidHandler = () => AddProgress(DailyQuestType.PayDebt);
                BankManager.Instance.OnDebtPaid += debtPaidHandler;
            }

            else BankManager.Instance.OnDebtPaid -= debtPaidHandler;
        }

        // ──────────────────────────────────────────────
        // 🪙 Coin Trading System
        // ──────────────────────────────────────────────
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

        // ──────────────────────────────────────────────
        // 📱 Post / Social System
        // ──────────────────────────────────────────────
        if (PostManager.Instance != null)
        {
            if (subscribe)
            {
                postCreatedHandler = () => AddProgress(DailyQuestType.CreatePosts);
                PostManager.Instance.OnPostCreated += postCreatedHandler;
            }

            else PostManager.Instance.OnPostCreated -= postCreatedHandler;
        }

        // ──────────────────────────────────────────────
        // 🛒 Shop System
        // ──────────────────────────────────────────────
        if (ShopManager.Instance != null)
        {
            if (subscribe)
            {
                itemPurchasedHandler = () => AddProgress(DailyQuestType.BuyFromShop);
                ShopManager.Instance.OnItemPurchased += itemPurchasedHandler;
            }

            else ShopManager.Instance.OnItemPurchased -= itemPurchasedHandler;
        }

        // ──────────────────────────────────────────────
        // 🧩 Debug Log
        // ──────────────────────────────────────────────
        Debug.Log($"[DailyQuestManager] {(subscribe ? "Subscribed" : "Unsubscribed")} to events.");
    }

    #endregion

    #region === Coroutines ===
    /// <summary>Checks for date change to refresh daily quests.</summary>
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
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
    #endregion
}
