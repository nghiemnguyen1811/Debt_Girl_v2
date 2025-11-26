using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(PlayerControl))]
public class PlayerStatsUI : MonoBehaviour
{
    #region === Serialized Fields ===

    [Header("UI Sliders")]
    [SerializeField] private Slider engagementSlider;
    [SerializeField] private Slider moodSlider;
    [SerializeField] private Slider energySlider;

    [Header("Tween Settings")]
    [SerializeField] private float tweenDuration = 0.25f;

    #endregion

    #region === Private Fields ===

    private Tween moodTween;
    private Tween energyTween;
    private Tween engagementTween;

    private PlayerControl control;
    private PlayerStats playerStats;

    #endregion

    #region === Initialization (Private) ===

    /// <summary>
    /// Cache PlayerControl and PlayerStats references.
    /// </summary>
    private bool SetupComponent()
    {
        if (this == null || gameObject == null)
            return false;

        if (!TryGetComponent(out control) || control == null)
            return false;

        playerStats = control.stats;

        return playerStats != null;
    }

    #endregion

    #region === Public Methods ===

    /// <summary>
    /// Initialize the maximum values for all stat sliders based on player stats.
    /// </summary>
    public void InitUI()
    {
        if (!SetupComponent())
        {
            Debug.LogWarning("PlayerStatsUI: SetupComponent failed — UI was destroyed or PlayerControl missing.");
            return;
        }

        SetSliderMaxValue(StatType.IncomeRate);
        SetSliderMaxValue(StatType.Productivity);
        SetSliderMaxValue(StatType.Mood);

        UpdateAll();
    }

    /// <summary>
    /// Update a specific stat slider based on the given StatType.
    /// </summary>
    /// <param name="type">The type of stat to update.</param>
    public void UpdateStatUI(StatType type)
    {
        switch (type)
        {
            case StatType.IncomeRate:
                UpdateSliderValue(ref engagementTween, engagementSlider, playerStats.engagement.current);
                break;

            case StatType.Productivity:
                UpdateSliderValue(ref energyTween, energySlider, playerStats.energy.current);
                break;

            case StatType.Mood:
                UpdateSliderValue(ref moodTween, moodSlider, playerStats.mood.current);
                break;
        }
    }

    /// <summary>
    /// Update all stat sliders (Mood, Energy, Engagement).
    /// </summary>
    public void UpdateAll()
    {
        UpdateStatUI(StatType.IncomeRate);
        UpdateStatUI(StatType.Productivity);
        UpdateStatUI(StatType.Mood);
    }

    #endregion

    #region === Private Methods ===

    /// <summary>
    /// Set the maximum value for a specific stat slider based on player stats.
    /// </summary>
    /// <param name="type">The type of stat to configure.</param>
    private void SetSliderMaxValue(StatType type)
    {
        if (playerStats == null) return;

        switch (type)
        {
            case StatType.IncomeRate:
                if (engagementSlider != null)
                    engagementSlider.maxValue = playerStats.playerStatsSO.maxEngagement;
                break;

            case StatType.Productivity:
                if (energySlider != null)
                    energySlider.maxValue = playerStats.GetScaledStatValue(playerStats.playerStatsSO.maxEnergy, type);
                break;

            case StatType.Mood:
                if (moodSlider != null)
                    moodSlider.maxValue = playerStats.GetScaledStatValue(playerStats.playerStatsSO.maxMood, type);
                break;
        }
    }

    /// <summary>
    /// Animate a slider's value smoothly using DOTween.
    /// </summary>
    /// <param name="tween">Reference to the tween instance for this slider.</param>
    /// <param name="slider">The UI slider to update.</param>
    /// <param name="value">The target value to animate to.</param>
    private void UpdateSliderValue(ref Tween tween, Slider slider, float value)
    {
        if (slider == null) return;

        tween?.Kill();
        tween = slider
            .DOValue(value, tweenDuration)
            .SetEase(Ease.OutCubic);
    }

    #endregion
}