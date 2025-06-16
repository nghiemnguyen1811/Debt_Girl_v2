using UnityEngine;

[CreateAssetMenu(fileName = "NewFaceMaterialSet", menuName = "Mood/Face Material Set")]
public class FaceMaterialSetSO : ScriptableObject
{
    [Header("Condition Type")]
    public MoodConditionType conditionType = MoodConditionType.None;

    [Header("Face Materials")]
    public Sprite eyeSprite;
    public Sprite mouthSprite;
}
