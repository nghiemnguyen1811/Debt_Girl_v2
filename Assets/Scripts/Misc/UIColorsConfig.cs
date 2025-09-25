using UnityEngine;

[CreateAssetMenu(menuName = "UI/UIColorsConfig")]
public class UIColorsConfig : ScriptableObject
{
    [Header("Ingredient Colors")]
    public Color textEnoughColor;
    public Color textNotEnoughColor;
    public Color frameEnoughColor;
    public Color frameNotEnoughColor;

    [Header("Plate Timer Colors")]
    public Color plateEmptyColor;
    public Color plateOccupiedColor;
}
