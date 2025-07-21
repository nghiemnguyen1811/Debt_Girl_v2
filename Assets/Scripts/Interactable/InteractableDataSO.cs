using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInteractableData", menuName = "Interactables/Interactable Data")]
public class InteractableDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // Interaction Settings
    // ─────────────────────────────────────────────────────
    [Title("General Settings")]
    [LabelText("Object Name")]
    public string objectName = "Object";

    [LabelText("Animation Name")]
    public string animationName = "Idle";

    [LabelText("Interaction Duration (s)")]
    [Min(0f)]
    public float interactionDuration = 2f;

    [LabelText("Play Animation Immediately")]
    public bool playAnimationImmediately = true;

    // ─────────────────────────────────────────────────────
    // Mood & Energy Effects
    // ─────────────────────────────────────────────────────
    [Title("Mood & Energy Effects")]
    [LabelText("Target Mood Type")]
    public MoodConditionType conditionType = MoodConditionType.None;

    [LabelText("Mood Amount")]
    [Range(-100f, 100f)]
    public float moodAmount = 0f;

    [LabelText("Energy Amount")]
    [Range(-100f, 100f)]
    public float energyAmount = 0f;

    // ─────────────────────────────────────────────────────
    // Income Settings
    // ─────────────────────────────────────────────────────
    [Title("Income Settings")]
    [LabelText("Earns Money")]
    public bool earnsMoney = false;

    [ShowIf(nameof(earnsMoney))]
    [LabelText("Money Earned")]
    [Min(0)]
    public double moneyEarned = 0;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (interactionDuration < 0f)
            interactionDuration = 0f;
    }
#endif
}
