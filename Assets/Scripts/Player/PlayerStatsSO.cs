using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Stats/Character Stats")]
public class PlayerStatsSO : ScriptableObject
{
    [Title("Engagement")]
    [FoldoutGroup("Engagement Settings", expanded: true)]
    [Range(0f, 100f)]
    public float maxEngagement = 100f;

    [Title("Energy")]
    [FoldoutGroup("Energy Settings", expanded: true)]
    [Range(0f, 100f)]
    public float maxEnergy = 100f;

    [Title("Mood")]
    [FoldoutGroup("Mood Settings", expanded: true)]
    [Range(0f, 100f)]
    public float maxMood = 100f;
}
