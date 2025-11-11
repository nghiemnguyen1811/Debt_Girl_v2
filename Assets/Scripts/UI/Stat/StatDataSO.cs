using UnityEngine;

[CreateAssetMenu(fileName = "New Stat Data", menuName = "Stats/StatDataSO")]
public class StatDataSO : ScriptableObject
{
    [Header("Stat Metadata")]
    public StatType statType;

    [Header("Display Info")]
    public Sprite icon;
    public string statNameKey;       // Localization key for name
    public string statDescriptionKey; // Localization key for description

    [Header("Runtime Data")]
    public int level = 0;
}