using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StatUpgradeManager : SingletonMonobehaviour<StatUpgradeManager>
{
    [Header("References")]
    [SerializeField] private PlayerControl playerControl;

    [Header("Data & Prefab")]
    [SerializeField] private StatDataSO[] statDataList;
    [SerializeField] private StatContainer statContainerPrefab;
    [SerializeField] private Transform containerParent;

    [Header("Stat Points")]
    [SerializeField] private int statPoints = 0;
    private int tempStatPoints = 0;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;

    private readonly List<StatContainer> spawnedContainers = new();

    // ─────────────────────────────────────────────────────
    // Mono
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        InitializeStatUI();
        SetupListeners();
    }

    private void SetupListeners()
    {
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyAll);
    }

    // ─────────────────────────────────────────────────────
    // UI Initialization
    // ─────────────────────────────────────────────────────

    private void InitializeStatUI()
    {
        spawnedContainers.Clear();

        foreach (StatDataSO statData in statDataList)
        {
            var container = Instantiate(statContainerPrefab, containerParent);
            container.Configure(statData);
            spawnedContainers.Add(container);
        }

        UpdateStatUpgradeUI();
        UpdateStatPointUI();
    }

    /// <summary>
    /// Update button states and apply button interactability for all stat upgrades.
    /// </summary>
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

    public void UpdateStatPointUI()
    {
        UIManager.Instance?.UpdateStatPoints(GetRemainingPoints());
    }

    private void UpdateApplyButtonState()
    {
        if (applyButton == null) return;

        bool hasPendingUpgrade = false;

        foreach (var container in spawnedContainers)
        {
            if (container.GetPendingLevel() > 0)
            {
                hasPendingUpgrade = true;
                break;
            }
        }

        applyButton.interactable = hasPendingUpgrade;
    }

    // ─────────────────────────────────────────────────────
    // Stat Upgrade Logic
    // ─────────────────────────────────────────────────────

    public void ApplyAll()
    {
        foreach (var container in spawnedContainers)
            container.CommitPendingLevel();

        ApplyStatPoints();
        UpdateStatUpgradeUI();
        UpdateStatPointUI();

        playerControl.stats.UpdateScaledStats();
    }

    public void ResetAll()
    {
        foreach (var container in spawnedContainers)
            container.ResetPendingLevel();

        ResetTempStatPoints();
        UpdateStatUpgradeUI();
        UpdateStatPointUI();
    }

    public void CancelUpgrade()
    {
        foreach (var container in spawnedContainers)
            container.ResetPendingLevel();

        ResetTempStatPoints();
        UpdateStatUpgradeUI();
        UpdateStatPointUI();
    }

    // ─────────────────────────────────────────────────────
    // Stat Point Management
    // ─────────────────────────────────────────────────────

    public void AddStatPoint()
    {
        statPoints++;
        UpdateStatUpgradeUI();
        UpdateStatPointUI();
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
    }

    private void ResetTempStatPoints()
    {
        tempStatPoints = 0;
    }

    private int GetRemainingPoints() => statPoints - tempStatPoints;

    public bool HasAvailablePoints() => statPoints - tempStatPoints > 0;

    // ─────────────────────────────────────────────────────
    // Stat Lookup
    // ─────────────────────────────────────────────────────

    public StatDataSO GetStatDataByType(StatType type)
    {
        foreach (var stat in statDataList)
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
        return container != null ? container.GetCurrentLevel() : 0;
    }
}
