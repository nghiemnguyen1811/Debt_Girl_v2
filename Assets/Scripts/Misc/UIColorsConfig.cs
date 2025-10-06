using UnityEngine;

[CreateAssetMenu(menuName = "UI/UIColorsConfig")]
public class UIColorsConfig : ScriptableObject
{
    [Header("Cooking")]
    public Color canCookColor;
    public Color cantCookColor;
    [Header("Ingredient Colors")]
    public Color textEnoughColor;
    public Color textNotEnoughColor;
    public Color frameEnoughColor;
    public Color frameNotEnoughColor;

    [Header("Plate Timer Colors")]
    public Color plateEmptyColor;
    public Color plateOccupiedColor;

    [Header("Tab Colors")]
    public Color tabOn;
    public Color tabOff;
}
