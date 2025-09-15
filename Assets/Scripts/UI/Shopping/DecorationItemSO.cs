using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Decoration Item", menuName = "Decorations/Item")]
public class DecorationItemSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // Display Info
    // ─────────────────────────────────────────────────────
    [Title("Decoration Info")]
    [HorizontalGroup("Top")]
    [PreviewField(60), HideLabel, GUIColor(0.9f, 0.9f, 1f)]
    public Sprite icon;

    [VerticalGroup("Top/Right"), LabelWidth(100)]
    public string itemName;

    [VerticalGroup("Top/Right"), LabelWidth(100)]
    [TextArea(2, 4)]
    public string description;

    // ─────────────────────────────────────────────────────
    // Ownership & Identity
    // ─────────────────────────────────────────────────────
    [Title("Ownership & Identity")]
    [LabelText("Item ID"), GUIColor(1f, 0.85f, 0.4f)]
    public int itemID;

    [LabelText("Owner Character")]
    public CharacterType owner;

    // ─────────────────────────────────────────────────────
    // Pricing
    // ─────────────────────────────────────────────────────
    [Title("Pricing")]
    [LabelText("Purchase Price"), SuffixLabel("$", true), MinValue(0)]
    public double price;
}
