using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Decoration Item", menuName = "Decorations/Item")]
public class DecorationItemSO : ScriptableObject
{
    [LabelText("Item ID"), GUIColor(1f, 0.85f, 0.4f)]
    public int itemID;

    [PreviewField(64)]
    public Sprite icon;

    public string itemName;

    public string description;

    public CharacterType owner;

    public double price;
}
