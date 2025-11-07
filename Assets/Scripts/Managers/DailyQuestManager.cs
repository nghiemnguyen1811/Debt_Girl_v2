using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
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
    public TextMeshProUGUI[] rewardTexts;
    public GameObject completedOverlay;

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
    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields ===
    [Header("Quest Database")]
    [SerializeField] private List<DailyQuestDataSO> questTemplates = new();
    [SerializeField] private int dailyQuestCount = 3;

    [Header("Runtime Data")]
    [SerializeField] private List<DailyQuestData> activeQuests = new();

    [Header("UI References")]
    [SerializeField] private QuestUIGroup[] questUIGroups;

    [Header("Completion Reward UI")]
    [SerializeField] private Slider overallProgressBar;
    [SerializeField] private GameObject completedOverlay;
    [SerializeField] private TextMeshProUGUI overallProgressText;
    [SerializeField] private TextMeshProUGUI[] bonusDiamondTexts;

    [SerializeField] private Button completionRewardEnable;
    [SerializeField] private Button completionRewardDisable;
    [SerializeField] private Button completionRewardClaimed;

    [Header("Claim All Reward UI")]
    [SerializeField] private Button claimAllEnable;
    [SerializeField] private Button claimAllDisable;

    [Header("Bonus Reward Settings")]
    [SerializeField] private int bonusDiamondAmount = 1;

    [Header("Registered Interactables")]
    [SerializeField] private List<InteractableBase> registeredInteractables = new();
    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Private Fields & Properties ===
    private string currentDate;
    private float checkInterval = 60f;
    private bool hasClaimedBonus;

    // Cached event handlers
    private Action cakeBakedHandler, dishCookedHandler, debtPaidHandler;
    private Action coinBoughtHandler, coinSellHandler, postCreatedHandler, itemPurchasedHandler;

    /// <summary>Gets player's current level (default = 1).</summary>
    private int PlayerLevel => GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Lifecycle ===
    private void Start() => InitializeSystem(); // Initialize quest system on start
    private void OnDestroy()
    {
        UnsubscribeEvents(); // Unsubscribe to avoid leaks
        StopAllCoroutines(); // Stop date check loop
    }
    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Initialization ===
    /// <summary>Initialize system, UI, and listeners.</summary>
    private void InitializeSystem()
    {
        currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        SubscribeToEvents();
        InitializeBonusUI();
        SetupListeners();
        RefreshUI();
        StartCoroutine(CheckDateChangeRoutine());
    }

    /// <summary>Set up button listeners for rewards and claim all.</summary>
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

    /// <summary>Set initial diamond bonus texts.</summary>
    private void InitializeBonusUI()
    {
        foreach (var bonusText in bonusDiamondTexts)
            bonusText.text = $"{bonusDiamondAmount}";
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


    //─────────────────────────────────────────────────────────────
    #region === Quest Generation ===
    /// <summary>Create new random daily quests.</summary>
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

    /// <summary>Filter quests available for current level.</summary>
    private List<DailyQuestDataSO> GetAvailableQuestsForLevel(int level)
        => questTemplates.FindAll(q => q.requiredLevel <= level);

    /// <summary>Randomly shuffle quest list.</summary>
    private List<DailyQuestDataSO> ShuffleList(List<DailyQuestDataSO> source)
    {
        var shuffled = new List<DailyQuestDataSO>(source);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int r = URandom.Range(i, shuffled.Count);
            (shuffled[i], shuffled[r]) = (shuffled[r], shuffled[i]);
        }
        return shuffled;
    }

    /// <summary>Instantiate daily quest data from templates.</summary>
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
                var valid = new List<DailyActivityRequirement>();
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

    //─────────────────────────────────────────────────────────────
    #region === Quest Progression ===
    /// <summary>Add progress to matching quest type.</summary>
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

    /// <summary>Check if quest progress is valid for player activity.</summary>
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

    //─────────────────────────────────────────────────────────────
    #region === Rewards (Single, All, Bonus) ===
    /// <summary>Claim one quest reward.</summary>
    public void ClaimQuestReward(int questIndex)
    {
        if (questIndex < 0 || questIndex >= activeQuests.Count) return;
        var quest = activeQuests[questIndex];
        if (!quest.isCompleted || quest.hasClaimedReward) return;

        quest.hasClaimedReward = true;
        MoneyManager.Instance.ChangeDiamonds(quest.questTemplate.rewardDiamond);
        AudioManager.Instance.PlayInteractSound(14);

        UpdateQuestUI(questIndex);
        UpdateOverallProgressUI();
        UpdateCompletionRewardUI();

        AutoSave();
    }

    /// <summary>Claim all completed quests and bonus reward.</summary>
    private void ClaimAllRewards()
    {
        int claimCount = 0;

        for (int i = 0; i < activeQuests.Count; i++)
        {
            var quest = activeQuests[i];
            if (quest.isCompleted && !quest.hasClaimedReward)
            {
                quest.hasClaimedReward = true;
                MoneyManager.Instance.ChangeDiamonds(quest.questTemplate.rewardDiamond);
                claimCount++;
                questUIGroups[i].completedOverlay?.SetActive(true);
            }
        }

        bool allCompleted = activeQuests.TrueForAll(q => q.isCompleted);
        if (allCompleted && !hasClaimedBonus)
        {
            hasClaimedBonus = true;
            MoneyManager.Instance.ChangeDiamonds(bonusDiamondAmount);
            claimCount++;
            completedOverlay?.SetActive(true);
        }

        if (claimCount > 0)
        {
            AudioManager.Instance.PlayInteractSound(14);
            AutoSave();
            RefreshUI();
            UpdateOverallProgressUI();
            UpdateCompletionRewardUI();
        }
    }

    /// <summary>Claim daily completion bonus reward.</summary>
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

        UpdateClaimAllButtons();
        UpdateCompletionRewardUI();

        completedOverlay?.SetActive(true);
        AutoSave();
    }
    #endregion

    //─────────────────────────────────────────────────────────────
    #region === UI Handling ===
    /// <summary>Refresh all quest UI and buttons.</summary>
    private void RefreshUI()
    {
        if (questUIGroups == null || questUIGroups.Length == 0) return;
        int questCount = activeQuests.Count;

        for (int i = 0; i < questUIGroups.Length; i++)
            UpdateQuestUI(i);

        UpdateClaimAllButtons();
        UpdateCompletionRewardUI();
        UpdateOverallProgressUI();
    }

    /// <summary>Update one quest UI by index.</summary>
    private void UpdateQuestUI(int index)
    {
        if (index < 0 || index >= questUIGroups.Length) return;
        if (index >= activeQuests.Count) return;

        var ui = questUIGroups[index];
        bool active = index < activeQuests.Count;
        ui.questGroup?.SetActive(active);
        if (!active) return;

        var quest = activeQuests[index];

        ui.descriptionText.text = quest.Description;
        ui.progressText.text = $"{quest.currentCount}/{quest.targetCount}";
        ui.completedOverlay?.SetActive(quest.isCompleted && quest.hasClaimedReward);

        float targetValue = (float)quest.currentCount / quest.targetCount;
        ui.progressBar.DOKill();
        ui.progressBar.DOValue(targetValue, 0.4f).SetEase(Ease.OutCubic);

        foreach (var rewardText in ui.rewardTexts)
            rewardText.text = $"{quest.questTemplate.rewardDiamond}";

        bool canClaim = quest.isCompleted && !quest.hasClaimedReward;
        bool alreadyClaimed = quest.isCompleted && quest.hasClaimedReward;

        ui.rewardButtonEnable.gameObject.SetActive(canClaim);
        ui.rewardButtonDisable.gameObject.SetActive(!quest.isCompleted);
        ui.claimedButton.gameObject.SetActive(alreadyClaimed);

        ui.rewardButtonEnable.onClick.RemoveAllListeners();
        ui.rewardButtonEnable.onClick.AddListener(() => ClaimQuestReward(index));
    }

    /// <summary>Update daily bonus button state.</summary>
    private void UpdateCompletionRewardUI()
    {
        bool allCompleted = activeQuests.TrueForAll(q => q.isCompleted);
        completionRewardEnable?.gameObject.SetActive(allCompleted && !hasClaimedBonus);
        completionRewardDisable?.gameObject.SetActive(!allCompleted);
        completionRewardClaimed?.gameObject.SetActive(allCompleted && hasClaimedBonus);
    }

    /// <summary>Update total progress bar and overlay.</summary>
    private void UpdateOverallProgressUI()
    {
        if (overallProgressBar == null || overallProgressText == null) return;

        if (activeQuests == null || activeQuests.Count == 0)
        {
            overallProgressBar.value = 0f;
            overallProgressText.text = "0/0";
            completedOverlay?.SetActive(false);
            return;
        }

        int completedCount = 0;
        foreach (var quest in activeQuests)
            if (quest.isCompleted) completedCount++;

        float progress = (float)completedCount / activeQuests.Count;

        overallProgressBar.DOKill();
        overallProgressBar.DOValue(progress, 0.5f).SetEase(Ease.OutCubic);
        overallProgressText.text = $"{completedCount}/{activeQuests.Count}";
        completedOverlay?.SetActive(hasClaimedBonus);
    }

    /// <summary>Update Claim All button state.</summary>
    private void UpdateClaimAllButtons()
    {
        int claimableCount = 0;
        foreach (var quest in activeQuests)
            if (quest.isCompleted && !quest.hasClaimedReward)
                claimableCount++;

        bool allCompleted = activeQuests.TrueForAll(q => q.isCompleted);
        if (allCompleted && !hasClaimedBonus)
            claimableCount++;

        bool canClaimAll = claimableCount >= 2;
        claimAllEnable?.gameObject.SetActive(canClaimAll);
        claimAllDisable?.gameObject.SetActive(!canClaimAll);
    }
    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Save & Load ===
    /// <summary>Load quest data from SaveManager.</summary>
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

    /// <summary>Save quest progress automatically.</summary>
    public void AutoSave(bool includeDate = false)
    {
        if (SaveManager.Data == null) return;
        SaveManager.Data.dailyQuests = activeQuests;
        SaveManager.Data.hasClaimedDailyBonus = hasClaimedBonus;
        if (includeDate) SaveManager.Data.lastQuestDate = currentDate;
        SaveManager.SaveGame();
    }

    /// <summary>Reset daily quests manually.</summary>
    public void ResetDailyQuests()
    {
        GenerateNewDailyQuests();
        AutoSave(true);
        RefreshUI();
    }
    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Event System ===
    private void SubscribeToEvents() => HandleEventSubscriptions(true); // Subscribe all gameplay events
    private void UnsubscribeEvents() => HandleEventSubscriptions(false); // Unsubscribe all events

    /// <summary>Bind or unbind all gameplay-related quest triggers.</summary>
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

        // Bank / Debt
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

        // Post System
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
    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Coroutines ===
    /// <summary>Check real-world date change every minute and reset quests.</summary>
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
