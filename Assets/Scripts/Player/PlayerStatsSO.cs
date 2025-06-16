using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Stats/Character Stats")]
public class PlayerStatsSO : ScriptableObject
{
    [Title("Experience")]
    [FoldoutGroup("Experience Settings", expanded: true)]
    [Range(0, 100)]
    public int maxExperience = 100;

    [Title("Energy")]
    [FoldoutGroup("Energy Settings", expanded: true)]
    [Range(0f, 100f)]
    public float maxEnergy = 100f;

    [Title("Mood")]
    [FoldoutGroup("Mood Settings", expanded: true)]
    [Range(0f, 100f)]
    public float maxMood = 100f;
}
