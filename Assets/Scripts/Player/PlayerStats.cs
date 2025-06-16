using UnityEngine;

[RequireComponent(typeof(PlayerControl))]
public class PlayerStats : MonoBehaviour
{
    [Header("Reference to Player Stats SO")]
    public PlayerStatsSO playerStatsSO;

    [HideInInspector] public StatValue mood = new StatValue();
    [HideInInspector] public StatValue energy = new StatValue();
    [HideInInspector] public int currentExperience;

    private float previousMood;
    private float previousEnergy;

    private PlayerControl control;

    private void Start()
    {
        control = GetComponent<PlayerControl>();

        if (!ValidateSetup()) return;

        InitializeStats();
        UpdateStatsUI();
    }

    private void UpdateStat(StatValue stat, float decayRate, ref float previousValue, System.Action updateUI)
    {
        stat.Subtract(decayRate * Time.deltaTime);

        if (stat.IsDifferentEnough(previousValue))
        {
            updateUI?.Invoke();
            previousValue = stat.current;
        }
    }

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

    private void InitializeStats()
    {
        mood.Init(playerStatsSO.maxMood);
        energy.Init(playerStatsSO.maxEnergy);
        currentExperience = 0;

        previousMood = mood.current;
        previousEnergy = energy.current;
    }

    private void UpdateStatsUI()
    {
        control.statsUI?.InitUI();
        control.statsUI?.UpdateAll();
    }

    public void ApplyMoodChange(float amount)
    {
        if (amount == 0) return;

        if (amount > 0)
            mood.Add(amount);

        else mood.Subtract(-amount);

        control.statsUI?.UpdateMoodUI();
        previousMood = mood.current;
    }

    public void ApplyEnergyChange(float amount)
    {
        if (amount == 0) return;

        if (amount > 0)
            energy.Add(amount);

        else energy.Subtract(-amount);

        control.statsUI?.UpdateEnergyUI();
        previousEnergy = energy.current;
    }

    public void GainExperience(int amount)
    {
        currentExperience += amount;
        currentExperience = Mathf.Clamp(currentExperience, 0, playerStatsSO.maxExperience);
        control.statsUI?.UpdateExperienceUI();
    }
}

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
