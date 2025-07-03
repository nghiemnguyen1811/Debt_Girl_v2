using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Controls the UI display of player stats: Mood, Energy, and Engagement.
/// Uses DOTween to animate slider transitions.
/// </summary>
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
    private PlayerStats playerStats => control.stats;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        control = GetComponent<PlayerControl>();
    }

    #endregion

    #region === Public Methods ===

    /// <summary>
    /// Initializes the UI sliders and sets their max values based on player stats.
    /// </summary>
    public void InitUI()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats reference is missing.");
            return;
        }

        if (engagementSlider != null)
            engagementSlider.maxValue = playerStats.playerStatsSO.maxEngagement;

        if (moodSlider != null)
            moodSlider.maxValue = playerStats.playerStatsSO.maxMood;

        if (energySlider != null)
            energySlider.maxValue = playerStats.playerStatsSO.maxEnergy;

        UpdateAll();
    }

    /// <summary>
    /// Smoothly updates the engagement slider.
    /// </summary>
    public void UpdateEngagementUI()
    {
        if (engagementSlider != null)
        {
            engagementTween?.Kill();
            engagementTween = engagementSlider
                .DOValue(playerStats.engagement.current, tweenDuration)
                .SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// Smoothly updates the mood slider.
    /// </summary>
    public void UpdateMoodUI()
    {
        if (moodSlider != null)
        {
            moodTween?.Kill();
            moodTween = moodSlider
                .DOValue(playerStats.mood.current, tweenDuration)
                .SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// Smoothly updates the energy slider.
    /// </summary>
    public void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energyTween?.Kill();
            energyTween = energySlider
                .DOValue(playerStats.energy.current, tweenDuration)
                .SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// Updates all three stat sliders (mood, energy, engagement).
    /// </summary>
    public void UpdateAll()
    {
        UpdateMoodUI();
        UpdateEnergyUI();
        UpdateEngagementUI();
    }

    #endregion
}
