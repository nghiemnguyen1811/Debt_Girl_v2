using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Stats/Character Stats")]
public class PlayerStatsSO : ScriptableObject
{
    [Title("Engagement")]
    [FoldoutGroup("Engagement Settings", expanded: true)]
    [Range(0f, 100f)]
    public float maxEngagement = 100f;

    [FoldoutGroup("Engagement Settings")]
    [MinValue(1), MaxValue(100)]
    public int engagementLevel = 1;

    [Title("Energy")]
    [FoldoutGroup("Energy Settings", expanded: true)]
    [Range(0f, 100f)]
    public float maxEnergy = 100f;

    [FoldoutGroup("Energy Settings")]
    [MinValue(1), MaxValue(100)]
    public int energyLevel = 1;

    [Title("Mood")]
    [FoldoutGroup("Mood Settings", expanded: true)]
    [Range(0f, 100f)]
    public float maxMood = 100f;

    [FoldoutGroup("Mood Settings")]
    [MinValue(1), MaxValue(100)]
    public int moodLevel = 1;
}
