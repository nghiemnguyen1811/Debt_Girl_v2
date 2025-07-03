using UnityEngine;
using System;

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

    // Called when the object is initialized
    private void Start()
    {
        control = GetComponent<PlayerControl>();

        if (!ValidateSetup()) return;

        InitializeStats();
        UpdateStatsUI();

        OnStatsInitialized?.Invoke();
    }

    #endregion

    #region === Initialization ===

    // Ensure playerStatsSO is assigned
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

    // Initialize all stat values
    private void InitializeStats()
    {
        engagement.Init(playerStatsSO.maxEngagement, 50f); // Start at 50%
        mood.Init(playerStatsSO.maxMood);
        energy.Init(playerStatsSO.maxEnergy);
    }

    // Initialize and refresh stat UI elements
    private void UpdateStatsUI()
    {
        control.statsUI?.InitUI();
        control.statsUI?.UpdateAll();
    }

    #endregion

    #region === Stat Modifiers ===

    // Modify mood value and update UI
    public void ApplyMoodChange(float amount)
    {
        if (amount == 0) return;

        if (amount > 0)
            mood.Add(amount);
        else
            mood.Subtract(-amount);

        control.statsUI?.UpdateMoodUI();
    }

    // Modify energy value and update UI
    public void ApplyEnergyChange(float amount)
    {
        if (amount == 0) return;

        if (amount > 0)
            energy.Add(amount);
        else
            energy.Subtract(-amount);

        control.statsUI?.UpdateEnergyUI();
    }

    // Modify engagement value and update UI
    public void ApplyEngagementChange(float amount)
    {
        if (amount == 0) return;

        if (amount > 0)
            engagement.Add(amount);
        else
            engagement.Subtract(-amount);

        control.statsUI?.UpdateEngagementUI();
    }

    #endregion
}

#region === StatValue Struct ===

[System.Serializable]
public class StatValue
{
    public float current;
    public float max;

    // Initialize with full value
    public void Init(float value)
    {
        current = value;
        max = value;
    }

    // Initialize with custom current and max
    public void Init(float maxValue, float currentValue)
    {
        max = maxValue;
        current = Mathf.Clamp(currentValue, 0, max);
    }

    // Set new max while clamping current
    public void SetMax(float newMax)
    {
        max = newMax;
        current = Mathf.Clamp(current, 0, max);
    }

    // Add to current with clamping
    public void Add(float amount)
    {
        current = Mathf.Clamp(current + amount, 0, max);
    }

    // Subtract from current with clamping
    public void Subtract(float amount)
    {
        current = Mathf.Clamp(current - amount, 0, max);
    }

    // Get current value as a percentage of max
    public float GetPercentage()
    {
        return max == 0 ? 0 : current / max;
    }

    // Check if the stat changed enough to matter
    public bool IsDifferentEnough(float previous, float threshold = 0.01f)
    {
        return Mathf.Abs(current - previous) > threshold;
    }
}

#endregion
