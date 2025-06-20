using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInteractableData", menuName = "Interactables/Interactable Data")]
public class InteractableDataSO : ScriptableObject
{
    [Title("Mood Condition")]
    [LabelText("Target Mood Type")]
    public MoodConditionType conditionType = MoodConditionType.None;

    [Title("General Settings")]
    [LabelText("Object Name")]
    public string objectName = "Object";

    [LabelText("Animation Name")]
    public string animationName = "Idle";

    [LabelText("Interaction Duration (s)")]
    public float interactionDuration = 2f;

    [Range(0, 500)]
    [LabelText("Experience Gained")]
    public int experienceAmount = 0;

    [Title("Affect Settings")]
    [LabelText("Affect Type")]
    public AffectType affectType = AffectType.None;

    [LabelText("Mood Amount")]
    [Range(-100f, 100f)]
    public float moodAmount = 0f;

    [LabelText("Energy Amount")]
    [Range(-100f, 100f)]
    public float energyAmount = 0f;

    [Title("Income Settings")]
    [LabelText("Earns Money")]
    public bool earnsMoney = false;

    [ShowIf(nameof(earnsMoney))]
    [LabelText("Money Earned")]
    [Min(0)]
    public int moneyEarned = 0;
}
