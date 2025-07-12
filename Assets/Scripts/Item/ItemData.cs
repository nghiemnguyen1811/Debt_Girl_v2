using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Title("Item Info")]
    [HorizontalGroup("Top")]
    [PreviewField(60), HideLabel, GUIColor(0.9f, 0.9f, 1f)]
    public Sprite icon;

    [VerticalGroup("Top/Right"), LabelWidth(100)]
    public string itemName;

    [TextArea(2, 4)]
    public string description;

    [Title("Item Properties")]
    public ItemType itemType;

    [ToggleLeft]
    public bool canStackItem = false;

    [EnableIf("canStackItem")]
    [LabelText("Max Stack"), Range(1, 999)]
    public int maxStackAmount = 1;
}
