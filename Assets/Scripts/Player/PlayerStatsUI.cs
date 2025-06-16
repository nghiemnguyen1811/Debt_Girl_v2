using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(PlayerControl))]
public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private Slider moodSlider;
    [SerializeField] private Slider energySlider;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private float tweenDuration = 0.25f;

    private Tween moodTween;
    private Tween energyTween;
    private Tween experienceTween;
    private PlayerControl control;

    private PlayerStats playerStats => control.stats;

    private void Start()
    {
        control = GetComponent<PlayerControl>();
    }

    public void InitUI()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats reference is missing.");
            return;
        }

        if (moodSlider != null)
            moodSlider.maxValue = playerStats.playerStatsSO.maxMood;

        if (energySlider != null)
            energySlider.maxValue = playerStats.playerStatsSO.maxEnergy;

        if (experienceSlider != null)
            experienceSlider.maxValue = playerStats.playerStatsSO.maxExperience;

        UpdateAll();
    }

    public void UpdateMoodUI()
    {
        if (moodSlider != null)
        {
            moodTween?.Kill();
            moodTween = moodSlider.DOValue(playerStats.mood.current, tweenDuration).SetEase(Ease.OutCubic);
        }
    }

    public void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energyTween?.Kill();
            energyTween = energySlider.DOValue(playerStats.energy.current, tweenDuration).SetEase(Ease.OutCubic);
        }
    }

    public void UpdateExperienceUI()
    {
        if (experienceSlider != null)
        {
            experienceTween?.Kill();
            experienceTween = experienceSlider.DOValue(playerStats.currentExperience, tweenDuration).SetEase(Ease.OutCubic);
        }
    }

    public void UpdateAll()
    {
        UpdateMoodUI();
        UpdateEnergyUI();
        UpdateExperienceUI();
    }
}
