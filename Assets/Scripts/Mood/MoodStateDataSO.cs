using UnityEngine;

[CreateAssetMenu(fileName = "NewMoodCondition", menuName = "Mood/Mood Condition Data")]
public class MoodConditionDataSO : ScriptableObject
{
    [Header("Condition Settings")]
    public MoodConditionType conditionType = MoodConditionType.None;

    [Header("Mood Visuals")]
    public Sprite moodIcon;

    [Header("Mood Animation Settings")]
    [Tooltip("The name of the animation clip played for this mood.")]
    public string moodAnimName;

    [Tooltip("Index of the Animator layer this mood will override (e.g., Sleepy Layer = 1).")]
    public int animatorLayerIndex = 1;

    [Header("Mood Effect Settings")]
    [Range(0f, 10f)]
    [Tooltip("The rate at which the mood decays over time.")]
    public float moodDecayRate = 1f;
}
