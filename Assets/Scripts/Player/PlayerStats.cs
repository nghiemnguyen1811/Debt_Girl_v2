using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(PlayerControl))]
public class PlayerStats : MonoBehaviour
{
    #region === References ===

    public PlayerStatsSO playerStatsSO;
    private PlayerControl control;

    #endregion

    #region === Stat Values ===

    [HideInInspector] public StatValue mood = new StatValue();
    [HideInInspector] public StatValue energy = new StatValue();
    [HideInInspector] public StatValue engagement = new StatValue();

    #endregion

    #region === Events ===

    public event Action OnStatsInitialized;

    #endregion

    #region === Unity Events ===

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
        OnStatsInitialized?.Invoke();
    }

    #endregion

    #region === Public Methods ===

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
    }

    /// <summary>
    /// Re-initialize max values for a specific stat based on upgrades.
    /// </summary>
    public void InitializeStatByType(StatType type)
    {
        switch (type)
        {
            case StatType.Mood:
                mood.SetMax(GetScaledStatValue(playerStatsSO.maxMood));
                break;

            case StatType.Productivity:
                energy.SetMax(GetScaledStatValue(playerStatsSO.maxEnergy));
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
    /// Initialize and refresh stat UI elements.
    /// </summary>
    public void UpdateStatsUI()
    {
        control.statsUI?.InitUI();
    }

    /// <summary>
    /// Calculate a scaled stat value based on base value and IncomeRate upgrade level.
    /// </summary>
    public float GetScaledStatValue(float baseValue)
    {
        int incomeRateLevel = StatUpgradeManager.Instance.GetLevelOf(StatType.IncomeRate);
        return baseValue + 10 * (incomeRateLevel - 1);
    }

    #endregion

    #region === Private Methods ===

    /// <summary>
    /// Initialize all stat values.
    /// </summary>
    private void InitializeStats()
    {
        engagement.Init(playerStatsSO.maxEngagement, 50f);
        energy.Init(GetScaledStatValue(playerStatsSO.maxEnergy));
        mood.Init(GetScaledStatValue(playerStatsSO.maxMood));
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

    #endregion
}

#region === StatValue Struct ===

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

#endregion
