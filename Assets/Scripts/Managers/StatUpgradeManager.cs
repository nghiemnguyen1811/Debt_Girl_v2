using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class StatUpgradeManager : SingletonMonobehaviour<StatUpgradeManager>
{
    [Header("References")]
    private PlayerControl playerControl;

    [Header("Data & Prefab")]
    [SerializeField] private StatDataSO[] statDataArray;
    [SerializeField] private StatContainer statContainerPrefab;
    [SerializeField] private Transform containerParent;

    [Header("Stat Points")]
    [SerializeField] private int statPoints = 0;
    private int tempStatPoints;

    [Header("Buttons")]
    [SerializeField] private Button applyButtonEnabled;
    [SerializeField] private Button applyButtonDisabled;

    private readonly List<StatContainer> spawnedContainers = new();

    //─────────────────────────────────────────────
    // Mono
    //─────────────────────────────────────────────

    private void OnEnable()
    {
        InitializeStatUI();
        SetupListeners();

        // Auto-refresh localized text on language change
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.RegisterForGlobalRefresh(RefreshAllLocalizedTexts);
    }

    private void Start()
    {
        playerControl = PlayerControl.Instance;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.UnregisterForGlobalRefresh(RefreshAllLocalizedTexts);
    }

    private void SetupListeners()
    {
        if (applyButtonEnabled != null)
            applyButtonEnabled.onClick.AddListener(ApplyAll);
    }

    //─────────────────────────────────────────────
    // UI Initialization
    //─────────────────────────────────────────────
    private void InitializeStatUI()
    {
        spawnedContainers.Clear();

        foreach (StatDataSO statData in statDataArray)
        {
            var container = Instantiate(statContainerPrefab, containerParent);
            container.Configure(statData);
            spawnedContainers.Add(container);
        }

        UpdateStatUpgradeUI();
        UpdateStatPointUI();
    }

    private void RefreshAllLocalizedTexts()
    {
        foreach (var container in spawnedContainers)
            container.RefreshLocalizedTexts();
    }

    public void UpdateStatUpgradeUI()
    {
        UpdateAllStatButtons();
        UpdateApplyButtonState();
    }

    private void UpdateAllStatButtons()
    {
        foreach (var container in spawnedContainers)
            container.UpdateButtonStates();
    }

    private void UpdateStatPointUI()
    {
        UIManager.Instance?.UpdateStatPoints(GetRemainingPoints());
    }

    private void UpdateApplyButtonState()
    {
        if (applyButtonEnabled == null || applyButtonDisabled == null) return;

        bool hasPendingUpgrade = false;

        foreach (var container in spawnedContainers)
        {
            if (container.GetPendingLevel() > 0)
            {
                hasPendingUpgrade = true;
                break;
            }
        }

        applyButtonEnabled.gameObject.SetActive(hasPendingUpgrade);
        applyButtonDisabled.gameObject.SetActive(!hasPendingUpgrade);
    }

    //─────────────────────────────────────────────
    // Stat Upgrade Logic
    //─────────────────────────────────────────────
    public void ApplyAll()
    {
        foreach (var container in spawnedContainers)
            container.CommitPendingLevel();

        ApplyStatPoints();
        UpdateStatUpgradeUI();
        UpdateStatPointUI();

        playerControl.stats.UpdateScaledStats();
        AudioManager.Instance.PlayInteractSound(8);
    }

    public void ResetAll()
    {
        foreach (var container in spawnedContainers)
            container.ResetPendingLevel();

        ResetTempStatPoints();
        UpdateStatUpgradeUI();
        UpdateStatPointUI();
    }

    //─────────────────────────────────────────────
    // Stat Point Management
    //─────────────────────────────────────────────
    public void AddStatPoint()
    {
        statPoints++;
        UpdateStatUpgradeUI();
        UpdateStatPointUI();
        AutoSave();
    }

    public bool TrySpendTempPoint()
    {
        if (tempStatPoints < statPoints)
        {
            tempStatPoints++;
            UpdateStatPointUI();
            return true;
        }
        return false;
    }

    public void RefundTempPoint()
    {
        if (tempStatPoints > 0)
        {
            tempStatPoints--;
            UpdateStatPointUI();
        }
    }

    private void ApplyStatPoints()
    {
        statPoints -= tempStatPoints;
        tempStatPoints = 0;
        AutoSave();
    }

    private void ResetTempStatPoints() => tempStatPoints = 0;
    private int GetRemainingPoints() => statPoints - tempStatPoints;
    public bool HasAvailablePoints() => statPoints - tempStatPoints > 0;

    // ─────────────────────────────────────────────────────
    // Stat Lookup
    // ─────────────────────────────────────────────────────
    public StatDataSO GetStatDataByType(StatType type)
    {
        foreach (var stat in statDataArray)
        {
            if (stat.statType == type)
                return stat;
        }
        return null;
    }

    public StatContainer GetContainerByType(StatType type)
    {
        return spawnedContainers.Find(c => c.GetStatType() == type);
    }

    public int GetLevelOf(StatType type)
    {
        var container = GetContainerByType(type);
        return container != null ? container.GetCurrentLevel() : 1;
    }

    //─────────────────────────────────────────────
    // Save/Load API
    //─────────────────────────────────────────────
    public void ImportSaveData(SaveData saveData)
    {
        if (saveData == null) return;

        statPoints = saveData.statPoints;
        tempStatPoints = 0;

        foreach (var s in saveData.statLevels)
        {
            var data = Array.Find(statDataArray, d => d.statType == s.statType);
            if (data != null) data.level = s.level;
        }

        foreach (var c in spawnedContainers)
            c.SyncFromData();

        PlayerControl.Instance.stats.UpdateScaledStats();
    }

    protected void AutoSave()
    {
        SaveManager.Data.statPoints = statPoints;
        SaveManager.Data.statLevels.Clear();

        foreach (var stat in statDataArray)
        {
            SaveManager.Data.statLevels.Add(new StatSaveData
            {
                statType = stat.statType,
                level = stat.level
            });
        }

        SaveManager.SaveGame();
    }
}
