using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(PlayerControl))]
public class PlayerStats : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // References
    // ─────────────────────────────────────────────────────
    public PlayerStatsSO playerStatsSO;
    private PlayerControl control;

    // ─────────────────────────────────────────────────────
    // Stat Values
    // ─────────────────────────────────────────────────────
    [HideInInspector] public StatValue mood = new StatValue();
    [HideInInspector] public StatValue energy = new StatValue();
    [HideInInspector] public StatValue engagement = new StatValue();

    // ─────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────
    public event Action OnStatsInitialized;
    public static event Action<PlayerStats> OnStatsReadyForLoad;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        control = GetComponent<PlayerControl>();

        if (!ValidateSetup()) return;

        InitializeStats();
        UpdateStatsUI();

        StartCoroutine(DelayedInvoke());
    }

    private IEnumerator DelayedInvoke()
    {
        yield return null;
        UpdateScaledStats();
        OnStatsReadyForLoad?.Invoke(this);
        OnStatsInitialized?.Invoke();
    }

    // ─────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Apply a stat value change based on StatType and amount.
    /// </summary>
    public void ApplyStatChange(StatType type, float amount)
    {
        if (amount == 0) return;

        switch (type)
        {
            case StatType.Mood:
                ModifyStat(ref mood, amount);
                break;

            case StatType.Productivity:
                ModifyStat(ref energy, amount);
                break;

            case StatType.IncomeRate:
                ModifyStat(ref engagement, amount);
                break;
        }

        control.statsUI?.UpdateStatUI(type);
        AutoSave();
    }

    /// <summary>
    /// Re-initialize max values for a specific stat based on upgrades.
    /// </summary>
    public void InitializeStatByType(StatType type)
    {
        switch (type)
        {
            case StatType.Mood:
                mood.SetMax(GetScaledStatValue(playerStatsSO.maxMood, type));
                break;

            case StatType.Productivity:
                energy.SetMax(GetScaledStatValue(playerStatsSO.maxEnergy, type));
                break;
        }
    }

    /// <summary>
    /// Re-initialize all upgradable stats and update UI.
    /// </summary>
    public void UpdateScaledStats()
    {
        InitializeStatByType(StatType.Mood);
        InitializeStatByType(StatType.Productivity);
        UpdateStatsUI();
    }

    /// <summary>
    /// Calculate a scaled stat value based on base value and upgrade level.
    /// </summary>
    public float GetScaledStatValue(float baseValue, StatType type)
    {
        int level = StatUpgradeManager.Instance.GetLevelOf(type);
        return baseValue + 10 * (level - 1);
    }

    /// <summary>
    /// Save current stat values into SaveData.
    /// </summary>
    public void AutoSave()
    {
        var data = SaveManager.Data;
        data.hasStats = true;
        data.moodCurrent = mood.current;
        data.energyCurrent = energy.current;
        data.engagementCurrent = engagement.current;

        SaveManager.SaveGame();
    }

    /// <summary>
    /// Load current stat values from SaveData.
    /// </summary>
    public void ImportSaveData(SaveData saveData)
    {
        if (saveData == null || !saveData.hasStats)
        {
            Debug.Log("[PlayerStats] No saved stats found → keep default initialized values.");
            return;
        }

        InitStat(mood, playerStatsSO.maxMood, StatType.Mood, saveData.moodCurrent);
        InitStat(energy, playerStatsSO.maxEnergy, StatType.Productivity, saveData.energyCurrent);
        InitStat(engagement, playerStatsSO.maxEngagement, StatType.IncomeRate, saveData.engagementCurrent);

        UpdateStatsUI();
    }

    // ─────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Initialize all stat values.
    /// </summary>
    private void InitializeStats()
    {
        InitStat(engagement, playerStatsSO.maxEngagement, StatType.IncomeRate, 50f);
        InitStat(energy, playerStatsSO.maxEnergy, StatType.Productivity);
        InitStat(mood, playerStatsSO.maxMood, StatType.Mood);
    }

    /// <summary>
    /// Common init logic for stats (scaled max + optional current).
    /// </summary>
    private void InitStat(StatValue stat, float baseMax, StatType type, float? currentValue = null)
    {
        float scaledMax = GetScaledStatValue(baseMax, type);

        if (currentValue.HasValue)
            stat.Init(scaledMax, currentValue.Value);

        else stat.Init(scaledMax);
    }

    /// <summary>
    /// Initialize and refresh stat UI elements.
    /// </summary>
    private void UpdateStatsUI()
    {
        control.statsUI?.InitUI();
    }

    /// <summary>
    /// Validate that PlayerStatsSO is assigned.
    /// </summary>
    private bool ValidateSetup()
    {
        if (playerStatsSO == null)
        {
            Debug.LogError("PlayerStatsSO is not assigned!");
            enabled = false;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Add or subtract a value from a stat.
    /// </summary>
    private void ModifyStat(ref StatValue stat, float amount)
    {
        if (amount > 0) stat.Add(amount);
        else stat.Subtract(-amount);
    }
}

// ─────────────────────────────────────────────────────
// StatValue Class
// ─────────────────────────────────────────────────────
[System.Serializable]
public class StatValue
{
    public float current;
    public float max;

    public void Init(float value)
    {
        current = value;
        max = value;
    }

    public void Init(float maxValue, float currentValue)
    {
        max = maxValue;
        current = Mathf.Clamp(currentValue, 0, max);
    }

    public void SetMax(float newMax)
    {
        max = newMax;
        current = Mathf.Clamp(current, 0, max);
    }

    public void Add(float amount)
    {
        current = Mathf.Clamp(current + amount, 0, max);
    }

    public void Subtract(float amount)
    {
        current = Mathf.Clamp(current - amount, 0, max);
    }

    public float GetPercentage()
    {
        return max == 0 ? 0 : current / max;
    }

    public bool IsDifferentEnough(float previous, float threshold = 0.01f)
    {
        return Mathf.Abs(current - previous) > threshold;
    }
}
