using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewSkinData", menuName = "Outfit/Skin Data", order = 0)]
public class SkinDataSO : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // 🔖 Identity
    // ─────────────────────────────────────────────────────
    [Title("Identification", bold: true)]
    [LabelText("Outfit Type")]
    public OutfitType skinType;

    // ─────────────────────────────────────────────────────
    // 📦 Basic Info
    // ─────────────────────────────────────────────────────
    [Title("Skin Info", bold: true)]
    [PreviewField(70, ObjectFieldAlignment.Left)]
    [HideLabel]
    public Sprite icon;

    [LabelText("Outfit Mesh")]
    [PreviewField(ObjectFieldAlignment.Left)]
    public Mesh outfitMesh;

    // ─────────────────────────────────────────────────────
    // 🎨 Visual Appearance
    // ─────────────────────────────────────────────────────
    [Title("Visual Materials", bold: true)]
    [LabelText("Outfit Materials")]
    [AssetsOnly, InlineEditor(InlineEditorModes.SmallPreview)]
    public Material[] outfitMaterials;

    [LabelText("Character Owner")]
    public CharacterType owner;

    // ─────────────────────────────────────────────────────
    // 💎 Currency Settings
    // ─────────────────────────────────────────────────────
    [Title("Economy", bold: true)]
    [LabelText("Is Default Skin")]
    [Tooltip("If true, this skin is automatically unlocked and equipped for its character.")]
    public bool isDefaultSkin = false;

    [ShowIf("@!isDefaultSkin")] // 🔹 Chỉ hiển thị khi KHÔNG phải skin mặc định
    [LabelText("Sell Price"), SuffixLabel("💎", overlay: true)]
    [MinValue(0)]
    public double sellPrice = 0;

    private void OnValidate()
    {
        // 🔹 Đảm bảo giá luôn về 0 nếu là skin mặc định
        if (isDefaultSkin)
            sellPrice = 0;
    }
}
