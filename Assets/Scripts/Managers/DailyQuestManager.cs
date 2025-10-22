using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────
// QUEST UI GROUP STRUCT
// ─────────────────────────────────────────────────────
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
}

// ─────────────────────────────────────────────────────
// DAILY QUEST MANAGER
// ─────────────────────────────────────────────────────
public class DailyQuestManager : SingletonMonobehaviour<DailyQuestManager>
{
    public event Action OnDailyQuestInitialized;

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
    [SerializeField] private TextMeshProUGUI bonusDiamondText;

    [Header("Bonus Reward Settings")]
    [SerializeField] private int bonusDiamondAmount = 1;

    // ─────────────────────────────────────────────────────
    // PRIVATE FIELDS
    // ─────────────────────────────────────────────────────
    private string currentDate;
    private float checkInterval = 60f;
    private bool hasClaimedBonus;

    // Cached event handlers (for future use)
    private Action cakeBakedHandler, dishCookedHandler, debtPaidHandler, coinBoughtHandler;
    private Action talkedHandler, levelChangedHandler, postCreatedHandler, itemPurchasedHandler;

    // ─────────────────────────────────────────────────────
    // PROPERTIES
    // ─────────────────────────────────────────────────────
    /// <summary>Returns the player's current level (default = 1 if GameManager not initialized).</summary>
    private int PlayerLevel => GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;

    // ─────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        OnDailyQuestInitialized?.Invoke();
        InitializeSystem();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        StopAllCoroutines();
    }

    // ─────────────────────────────────────────────────────
    // INITIALIZATION
    // ─────────────────────────────────────────────────────
    /// <summary>Sets up the quest system, UI, and daily check routines.</summary>
    private void InitializeSystem()
    {
        currentDate = DateTime.Now.ToString("yyyy-MM-dd");

        SubscribeToEvents();
        InitializeBonusUI();
        SetupListeners();
        RefreshUI();
        StartCoroutine(CheckDateChangeRoutine());
    }

    private void SetupListeners()
    {
        if (completionRewardEnable == null) return;

        completionRewardEnable.onClick.RemoveAllListeners();
        completionRewardEnable.onClick.AddListener(ClaimDailyBonus);
    }

    private void InitializeBonusUI()
    {
        if (bonusDiamondText != null)
            bonusDiamondText.text = $"{bonusDiamondAmount}";
    }

    // ─────────────────────────────────────────────────────
    // BONUS CLAIMING
    // ─────────────────────────────────────────────────────
    /// <summary>Handles claiming the daily diamond bonus after all quests are done.</summary>
    private void ClaimDailyBonus()
    {
        bool allCompleted = activeQuests.TrueForAll(q => q.isCompleted);

        if (!allCompleted)
        {
            Debug.Log("[DailyQuestManager] Can't claim bonus — not all quests completed.");
            return;
        }

        if (hasClaimedBonus)
        {
            Debug.Log("[DailyQuestManager] Bonus already claimed for today.");
            return;
        }

        // Reward player (optional)
        // PlayerStats.Instance.AddDiamonds(bonusDiamondAmount);

        // Update claimed flag (no direct SaveManager usage)
        hasClaimedBonus = true;

        // Save everything through AutoSave()
        AutoSave();

        // Refresh UI
        UpdateCompletionRewardUI();
        Debug.Log($"[DailyQuestManager] Claimed {bonusDiamondAmount} diamonds bonus!");
    }

    // ─────────────────────────────────────────────────────
    // QUEST GENERATION
    // ─────────────────────────────────────────────────────
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

    private List<DailyQuestDataSO> GetAvailableQuestsForLevel(int playerLevel)
        => questTemplates.FindAll(q => q.requiredLevel <= playerLevel);

    private List<DailyQuestDataSO> ShuffleList(List<DailyQuestDataSO> source)
    {
        List<DailyQuestDataSO> shuffled = new(source);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
        }
        return shuffled;
    }

    private void CreateActiveQuestsFromTemplates(List<DailyQuestDataSO> shuffled, int playerLevel)
    {
        int questToAdd = Mathf.Min(dailyQuestCount, shuffled.Count);

        for (int i = 0; i < questToAdd; i++)
        {
            var template = shuffled[i];

            // Interact quests
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

                var chosenActivity = validActivities[UnityEngine.Random.Range(0, validActivities.Count)];

                activeQuests.Add(new DailyQuestData
                {
                    questTemplate = template,
                    targetCount = UnityEngine.Random.Range(template.minTarget, template.maxTarget + 1),
                    currentCount = 0,
                    isCompleted = false,
                    selectedActivity = chosenActivity.activity
                });

                Debug.Log($"[DailyQuestManager] Interact quest → chose activity: {chosenActivity.activity}");
                continue;
            }

            // Normal quests
            activeQuests.Add(new DailyQuestData
            {
                questTemplate = template,
                targetCount = UnityEngine.Random.Range(template.minTarget, template.maxTarget + 1),
                currentCount = 0,
                isCompleted = false
            });
        }
    }

    // ─────────────────────────────────────────────────────
    // QUEST PROGRESSION
    // ─────────────────────────────────────────────────────
    public void AddProgress(DailyQuestType type, DailyActivity activity = DailyActivity.None)
    {
        bool changed = false;

        foreach (var quest in activeQuests)
        {
            if (!CanProgressQuest(quest, type, activity, PlayerLevel))
                continue;

            int before = quest.currentCount;
            quest.AddProgress();

            if (quest.currentCount != before)
                changed = true;
        }

        if (changed)
        {
            RefreshUI();
            AutoSave();
        }
    }

    private bool CanProgressQuest(DailyQuestData quest, DailyQuestType type, DailyActivity activity, int playerLevel)
    {
        if (quest == null || quest.questTemplate == null) return false;
        if (quest.questTemplate.requiredLevel > playerLevel) return false;

        if (quest.questTemplate.questType != DailyQuestType.Interact)
            return quest.questTemplate.questType == type;

        if (type != DailyQuestType.Interact || quest.selectedActivity == DailyActivity.None)
            return false;

        return quest.selectedActivity == activity;
    }

    // ─────────────────────────────────────────────────────
    // UI HANDLING
    // ─────────────────────────────────────────────────────
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
        }

        for (int i = 0; i < separatorLines.Length; i++)
            separatorLines[i]?.SetActive(i < questCount - 1);

        UpdateCompletionRewardUI();
    }

    private void UpdateCompletionRewardUI()
    {
        bool allCompleted = activeQuests.TrueForAll(q => q.isCompleted);

        completionRewardEnable?.gameObject.SetActive(allCompleted && !hasClaimedBonus);
        completionRewardDisable?.gameObject.SetActive(!allCompleted);
        completionRewardClaimed?.gameObject.SetActive(allCompleted && hasClaimedBonus);
    }

    // ─────────────────────────────────────────────────────
    // SAVE & LOAD
    // ─────────────────────────────────────────────────────
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
            GenerateNewDailyQuests();
            AutoSave(true);
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

    // ─────────────────────────────────────────────────────
    // EVENT SUBSCRIPTIONS
    // ─────────────────────────────────────────────────────
    private void SubscribeToEvents() { }
    private void UnsubscribeEvents() { }

    // ─────────────────────────────────────────────────────
    // COROUTINES
    // ─────────────────────────────────────────────────────
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
