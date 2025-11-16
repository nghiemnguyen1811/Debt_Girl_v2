using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using URandom = UnityEngine.Random;
/// <summary>
/// UI container used by DailyQuestManager to display one quest entry.
/// Holds all UI elements for description, progress, reward display,
/// completion overlay, and reward claim buttons.
/// </summary>
[Serializable]
public class QuestUIGroup
{
    // ---------------------------------------------------------
    // UI Elements
    // ---------------------------------------------------------

    [Header("Quest UI Elements")]
    public GameObject questGroup;                 // Root object of this quest UI slot
    public TextMeshProUGUI descriptionText;       // Localized quest description
    public Slider progressBar;                    // Progress bar for currentCount / targetCount
    public TextMeshProUGUI progressText;          // Progress numeric text (e.g., "2/5")
    public TextMeshProUGUI[] rewardTexts;         // Reward value (usually diamonds)
    public GameObject completedOverlay;           // Overlay shown when reward is claimed

    // ---------------------------------------------------------
    // Reward Buttons
    // ---------------------------------------------------------

    [Header("Reward Buttons")]
    public Button rewardButtonEnable;             // Button shown when the quest can be claimed
    public Button rewardButtonDisable;            // Button shown when quest not finished yet
    public Button claimedButton;                  // Button/indicator shown after claiming reward
}
/// <summary>
/// Controls generation, progression, UI updates, saving and localization
/// of the Daily Quest system.
/// </summary>
public class DailyQuestManager : SingletonMonobehaviour<DailyQuestManager>
{
    #region Inspector Fields

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

    #region Private Fields

    private string currentDate;
    private float checkInterval = 60f;
    private bool hasClaimedBonus;

    // Cached event handlers
    private Action cakeBakedHandler;
    private Action dishCookedHandler;
    private Action debtPaidHandler;
    private Action coinBoughtHandler;
    private Action coinSellHandler;
    private Action postCreatedHandler;
    private Action itemPurchasedHandler;

    private int PlayerLevel =>
        GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;

    #endregion

    #region Unity Lifecycle

    private void Start() => InitializeSystem();

    private void OnDestroy()
    {
        UnsubscribeEvents();
        LocalizationManager.Instance.UnregisterForGlobalRefresh(OnLanguageChanged);
        StopAllCoroutines();
    }
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the quest system, restores data,
    /// subscribes events and prepares UI.
    /// </summary>
    private void InitializeSystem()
    {
        currentDate = DateTime.Now.ToString("yyyy-MM-dd");

        SubscribeToEvents();
        InitializeBonusUI();
        SetupButtonListeners();
        LocalizationManager.Instance.RegisterForGlobalRefresh(OnLanguageChanged);
        RefreshUI();

        StartCoroutine(CheckDateChangeRoutine());
    }

    /// <summary>
    /// Wire up all UI button listeners.
    /// </summary>
    private void SetupButtonListeners()
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

    /// <summary>
    /// Initializes diamond bonus text fields.
    /// </summary>
    private void InitializeBonusUI()
    {
        foreach (var txt in bonusDiamondTexts)
            txt.text = $"{bonusDiamondAmount}";
    }

    #endregion

    #region Interactable Registration

    /// <summary>
    /// Registers interactables for progress events.
    /// </summary>
    public void RegisterInteractable(InteractableBase interactable)
    {
        if (!registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Add(interactable);
            interactable.OnStopInteractable += AddProgress;
        }
    }

    /// <summary>
    /// Removes interactables from listeners.
    /// </summary>
    public void UnregisterInteractable(InteractableBase interactable)
    {
        if (registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Remove(interactable);
            interactable.OnStopInteractable -= AddProgress;
        }
    }

    #endregion

    #region Quest Generation

    /// <summary>
    /// Clears and generates a fresh list of daily quests.
    /// </summary>
    private void GenerateNewDailyQuests()
    {
        activeQuests.Clear();
        var available = GetAvailableQuestsForLevel(PlayerLevel);

        if (available.Count == 0)
        {
            Debug.LogWarning($"No quests unlocked for level {PlayerLevel}");
            return;
        }

        var shuffled = ShuffleList(available);
        CreateActiveQuests(shuffled, PlayerLevel);
        AutoSave();
    }

    /// <summary>
    /// Returns all templates unlocked at this level.
    /// </summary>
    private List<DailyQuestDataSO> GetAvailableQuestsForLevel(int level)
        => questTemplates.FindAll(q => q.requiredLevel <= level);

    /// <summary>
    /// Returns a randomly shuffled version of the list.
    /// </summary>
    private List<DailyQuestDataSO> ShuffleList(List<DailyQuestDataSO> source)
    {
        var result = new List<DailyQuestDataSO>(source);
        for (int i = 0; i < result.Count; i++)
        {
            int r = URandom.Range(i, result.Count);
            (result[i], result[r]) = (result[r], result[i]);
        }
        return result;
    }

    /// <summary>
    /// Generates runtime quest objects from templates.
    /// </summary>
    private void CreateActiveQuests(List<DailyQuestDataSO> templates, int level)
    {
        int count = Mathf.Min(dailyQuestCount, templates.Count);

        for (int i = 0; i < count; i++)
        {
            var template = templates[i];

            // Interact-type quests
            if (template.questType == DailyQuestType.Interact &&
                template.activityRequirements != null &&
                template.activityRequirements.Length > 0)
            {
                var validReq = new List<DailyActivityRequirement>();
                foreach (var req in template.activityRequirements)
                    if (req.requiredLevel <= level)
                        validReq.Add(req);

                if (validReq.Count == 0)
                    continue;

                var chosen = validReq[URandom.Range(0, validReq.Count)];

                activeQuests.Add(new DailyQuestData
                {
                    questID = template.name,
                    questTemplate = template,
                    targetCount = URandom.Range(template.minTarget, template.maxTarget + 1),
                    selectedActivity = chosen.activity,
                    savedActivityInt = (int)chosen.activity,
                    currentCount = 0,
                    isCompleted = false
                });

                continue;
            }

            // Normal quests
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

    #region Quest Progression

    /// <summary>
    /// Adds progress to quests matching the given type and activity.
    /// </summary>
    public void AddProgress(DailyQuestType type, DailyActivity activity = DailyActivity.None)
    {
        bool updated = false;

        foreach (var quest in activeQuests)
        {
            if (!CanProgressQuest(quest, type, activity, PlayerLevel))
                continue;

            int before = quest.currentCount;
            quest.AddProgress();
            if (quest.currentCount != before)
                updated = true;
        }

        if (updated)
        {
            RefreshUI();
            AutoSave();
        }
    }

    /// <summary>
    /// Determines if a quest should receive progress.
    /// </summary>
    private bool CanProgressQuest(DailyQuestData quest, DailyQuestType type, DailyActivity activity, int level)
    {
        if (quest == null || quest.questTemplate == null)
            return false;

        if (quest.questTemplate.requiredLevel > level)
            return false;

        if (quest.isCompleted)
            return false;

        if (quest.questTemplate.questType != DailyQuestType.Interact)
            return quest.questTemplate.questType == type;

        if (type != DailyQuestType.Interact)
            return false;

        return quest.selectedActivity == activity;
    }

    #endregion

    #region Rewards

    /// <summary>
    /// Claims reward for a single quest.
    /// </summary>
    public void ClaimQuestReward(int index)
    {
        if (index < 0 || index >= activeQuests.Count)
            return;

        var quest = activeQuests[index];
        if (!quest.isCompleted || quest.hasClaimedReward)
            return;

        quest.hasClaimedReward = true;
        MoneyManager.Instance.ChangeDiamonds(quest.questTemplate.rewardDiamond);
        AudioManager.Instance.PlayInteractSound(14);

        UpdateQuestUI(index);
        UpdateOverallProgressUI();
        UpdateCompletionRewardUI();

        AutoSave();
    }

    /// <summary>
    /// Claims all completed quest rewards and bonus if eligible.
    /// </summary>
    private void ClaimAllRewards()
    {
        int rewardCount = 0;

        for (int i = 0; i < activeQuests.Count; i++)
        {
            var quest = activeQuests[i];
            if (quest.isCompleted && !quest.hasClaimedReward)
            {
                quest.hasClaimedReward = true;
                MoneyManager.Instance.ChangeDiamonds(quest.questTemplate.rewardDiamond);
                rewardCount++;
            }
        }

        // Bonus
        bool allDone = activeQuests.TrueForAll(q => q.isCompleted);
        if (allDone && !hasClaimedBonus)
        {
            hasClaimedBonus = true;
            MoneyManager.Instance.ChangeDiamonds(bonusDiamondAmount);
            rewardCount++;
        }

        if (rewardCount > 0)
        {
            AudioManager.Instance.PlayInteractSound(14);
            AutoSave();
            RefreshUI();
        }
    }

    /// <summary>
    /// Claims the final completion bonus.
    /// </summary>
    private void ClaimDailyBonus()
    {
        if (hasClaimedBonus)
            return;

        hasClaimedBonus = true;
        MoneyManager.Instance.ChangeDiamonds(bonusDiamondAmount);

        UpdateClaimAllButtons();
        UpdateCompletionRewardUI();

        completedOverlay?.SetActive(true);
        AutoSave();
    }

    #endregion

    #region UI Handling

    /// <summary>
    /// Refreshes all UI: quests, buttons, progress bars.
    /// </summary>
    private void RefreshUI()
    {
        if (questUIGroups == null || questUIGroups.Length == 0)
            return;

        for (int i = 0; i < questUIGroups.Length; i++)
            UpdateQuestUI(i);

        UpdateClaimAllButtons();
        UpdateCompletionRewardUI();
        UpdateOverallProgressUI();
    }

    /// <summary>
    /// Updates a single quest UI element.
    /// </summary>
    private void UpdateQuestUI(int index)
    {
        if (index < 0 || index >= questUIGroups.Length)
            return;

        var ui = questUIGroups[index];
        bool active = index < activeQuests.Count;
        ui.questGroup?.SetActive(active);

        if (!active)
            return;

        var quest = activeQuests[index];

        // Localized description
        StartCoroutine(UpdateQuestDescriptionAsync(ui.descriptionText, quest));

        ui.progressText.text = $"{quest.currentCount}/{quest.targetCount}";
        ui.completedOverlay?.SetActive(quest.isCompleted && quest.hasClaimedReward);

        float t = (float)quest.currentCount / quest.targetCount;
        ui.progressBar.DOKill();
        ui.progressBar.DOValue(t, 0.4f).SetEase(Ease.OutCubic);

        foreach (var r in ui.rewardTexts)
            r.text = $"{quest.questTemplate.rewardDiamond}";

        bool canClaim = quest.isCompleted && !quest.hasClaimedReward;
        bool claimed = quest.isCompleted && quest.hasClaimedReward;

        ui.rewardButtonEnable.gameObject.SetActive(canClaim);
        ui.rewardButtonDisable.gameObject.SetActive(!quest.isCompleted);
        ui.claimedButton.gameObject.SetActive(claimed);

        ui.rewardButtonEnable.onClick.RemoveAllListeners();
        ui.rewardButtonEnable.onClick.AddListener(() => ClaimQuestReward(index));
    }

    private IEnumerator UpdateQuestDescriptionAsync(TextMeshProUGUI label, DailyQuestData quest)
    {
        var task = quest.GetLocalizedDescriptionAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        label.text = task.Result;
    }

    /// <summary>
    /// Updates bonus reward UI.
    /// </summary>
    private void UpdateCompletionRewardUI()
    {
        bool allCompleted = activeQuests.TrueForAll(q => q.isCompleted);
        completionRewardEnable?.gameObject.SetActive(allCompleted && !hasClaimedBonus);
        completionRewardDisable?.gameObject.SetActive(!allCompleted);
        completionRewardClaimed?.gameObject.SetActive(allCompleted && hasClaimedBonus);
    }

    /// <summary>
    /// Updates total progress bar for daily quests.
    /// </summary>
    private void UpdateOverallProgressUI()
    {
        if (overallProgressBar == null || overallProgressText == null)
            return;

        if (activeQuests.Count == 0)
        {
            overallProgressBar.value = 0f;
            overallProgressText.text = "0/0";
            completedOverlay?.SetActive(false);
            return;
        }

        int completed = 0;
        foreach (var q in activeQuests)
            if (q.isCompleted)
                completed++;

        float progress = (float)completed / activeQuests.Count;

        overallProgressBar.DOKill();
        overallProgressBar.DOValue(progress, 0.5f).SetEase(Ease.OutCubic);

        overallProgressText.text = $"{completed}/{activeQuests.Count}";
        completedOverlay?.SetActive(hasClaimedBonus);
    }

    /// <summary>
    /// Enables or disables the "Claim All" button.
    /// </summary>
    private void UpdateClaimAllButtons()
    {
        int claimable = 0;

        foreach (var quest in activeQuests)
        {
            if (quest.isCompleted && !quest.hasClaimedReward)
                claimable++;
        }

        if (activeQuests.TrueForAll(q => q.isCompleted) && !hasClaimedBonus)
            claimable++;

        bool canClaimAll = claimable >= 2;

        claimAllEnable?.gameObject.SetActive(canClaimAll);
        claimAllDisable?.gameObject.SetActive(!canClaimAll);
    }

    /// <summary>
    /// Called when the game language changes.
    /// Clears cached descriptions and refreshes all quest UI.
    /// </summary>
    private void OnLanguageChanged()
    {
        foreach (var quest in activeQuests)
            quest.ClearCachedDescription();

        RefreshUI();
    }
    #endregion

    #region Save & Load

    /// <summary>
    /// Loads quest data from SaveManager.
    /// </summary>
    public void ImportSaveData(SaveData data)
    {
        if (data == null)
            return;

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

        // Restore template references
        foreach (var quest in activeQuests)
        {
            if (quest.questTemplate == null && !string.IsNullOrEmpty(quest.questID))
                quest.questTemplate = questTemplates.Find(q => q.name == quest.questID);

            quest.selectedActivity = (DailyActivity)quest.savedActivityInt;
        }

        RefreshUI();
    }

    /// <summary>
    /// Saves current quest state.
    /// </summary>
    public void AutoSave(bool includeDate = false)
    {
        if (SaveManager.Data == null)
            return;

        SaveManager.Data.dailyQuests = activeQuests;
        SaveManager.Data.hasClaimedDailyBonus = hasClaimedBonus;

        if (includeDate)
            SaveManager.Data.lastQuestDate = currentDate;

        SaveManager.SaveGame();
    }

    /// <summary>
    /// Forces a complete daily quest reset.
    /// </summary>
    public void ResetDailyQuests()
    {
        GenerateNewDailyQuests();
        AutoSave(true);
        RefreshUI();
    }

    #endregion

    #region Event System

    private void SubscribeToEvents() => SetEventSubscriptions(true);

    private void UnsubscribeEvents() => SetEventSubscriptions(false);

    /// <summary>
    /// Handles binding/unbinding of all gameplay-related quest triggers.
    /// </summary>
    private void SetEventSubscriptions(bool subscribe)
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

        // Debt
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
    }

    #endregion

    #region Coroutines

    /// <summary>
    /// Periodically checks if the real-world date changed.
    /// If so, resets quests for the new day.
    /// </summary>
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
