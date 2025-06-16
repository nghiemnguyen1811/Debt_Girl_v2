using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInteractableData", menuName = "Interactables/Interactable Data")]
public class InteractableDataSO : ScriptableObject
{
    [Title("General Settings")]
    public string objectName = "Object";
    public string animationName = "Idle";
    public float interactionDuration = 2f;

    [Range(0, 500)]
    [LabelText("Experience Gained")]
    public int experienceAmount = 0;

    [Title("Effect Settings")]
    public AffectType affectType = AffectType.None;

    [ShowIf(nameof(ShowMood))]
    [Range(-100f, 100f)]
    public float moodAmount = 0f;

    [ShowIf(nameof(ShowEnergy))]
    [Range(-100f, 100f)]
    public float energyAmount = 0f;

    private bool ShowMood()
    {
        return affectType == AffectType.Mood || affectType == AffectType.Both;
    }

    private bool ShowEnergy()
    {
        return affectType == AffectType.Energy || affectType == AffectType.Both;
    }
}
