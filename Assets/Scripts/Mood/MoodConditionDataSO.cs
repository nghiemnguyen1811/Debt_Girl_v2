using UnityEngine;

[CreateAssetMenu(fileName = "NewMoodCondition", menuName = "Mood/Mood Condition Data")]
public class MoodConditionDataSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("The type of mood this condition represents.")]
    public MoodConditionType conditionType = MoodConditionType.None;

    [Header("Visual & Animation")]
    [Tooltip("Icon representing this mood in the UI.")]
    public Sprite moodIcon;

    [Tooltip("The name of the animation trigger for this mood.")]
    public string moodAnimName;

    [Tooltip("Animator layer index that this mood animation will override (e.g., Sleepy = 1).")]
    public int animatorLayerIndex = 1;

    [Header("Mood Effect")]
    [Tooltip("Rate at which the mood stat decays over time (per second).")]
    [Range(0f, 10f)]
    public float moodDecayRate = 1f;

    [Header("Trigger Timing")]
    [Tooltip("Minimum time (in seconds) before this mood can be triggered.")]
    public float minTime = 180f;

    [Tooltip("Maximum time (in seconds) before this mood can be triggered.")]
    public float maxTime = 300f;
}
