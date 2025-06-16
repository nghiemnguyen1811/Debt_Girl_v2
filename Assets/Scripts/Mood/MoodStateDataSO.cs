using UnityEngine;

[CreateAssetMenu(fileName = "NewMoodCondition", menuName = "Mood/Mood Condition Data")]
public class MoodConditionDataSO : ScriptableObject
{
    [Header("Condition Settings")]
    public MoodConditionType conditionType = MoodConditionType.None;

    [Header("Mood Visuals")]
    public Sprite moodIcon;

    [Header("Mood Effect Settings")]
    [Range(0f, 10f)]
    public float moodDecayRate = 1f;
}