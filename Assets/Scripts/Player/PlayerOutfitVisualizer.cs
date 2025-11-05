using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Applies equipped meshes and materials for in-game and preview models.
/// </summary>
public class PlayerOutfitVisualizer : MonoBehaviour
{
    #region === Serialized Fields ===

    [Header("Character Visual Sets")]
    [SerializeField] private List<CharacterVisualSet> visualSets = new();

    #endregion

    #region === Private Fields ===

    private PlayerControl control;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        control = GetComponent<PlayerControl>();
    }

    #endregion

    #region === Public Utility ===

    /// <summary>
    /// Apply equipped outfits for the given character.
    /// </summary>
    public void ApplyOutfits(CharacterType character, List<EquippedOutfitEntry> equippedList)
    {
        if (equippedList == null || equippedList.Count == 0) return;

        var filteredList = equippedList.FindAll(e => e.owner == character);
        if (filteredList.Count == 0) return;

        var visualSet = visualSets.Find(v => v.characterType == character);
        if (visualSet == null) return;

        foreach (var entry in filteredList)
        {
            switch (entry.outfitType)
            {
                case OutfitType.Shirt:
                    ApplyMeshAndMaterial(visualSet.gameShirtRenderer, visualSet.previewShirtRenderer, entry.skinID);
                    break;
                case OutfitType.Pant:
                    ApplyMeshAndMaterial(visualSet.gamePantRenderer, visualSet.previewPantRenderer, entry.skinID);
                    break;
                case OutfitType.Shoes:
                    ApplyMeshAndMaterial(visualSet.gameShoesRenderer, visualSet.previewShoesRenderer, entry.skinID);
                    break;
            }
        }

        control.animationHandler.PlayPreviewAnimation("ChangeOutfit");
    }

    #endregion

    #region === Helpers ===

    /// <summary>
    /// Apply mesh and material to both renderers.
    /// </summary>
    private void ApplyMeshAndMaterial(SkinnedMeshRenderer gameRenderer, SkinnedMeshRenderer previewRenderer, string skinID)
    {
        if (OutfitManager.Instance == null) return;

        var skin = OutfitManager.Instance.GetSkinDataByID(skinID);
        if (skin == null || skin.outfitMesh == null) return;

        ApplyToRenderer(gameRenderer, skin);
        ApplyToRenderer(previewRenderer, skin);
    }

    /// <summary>
    /// Assign mesh and materials to renderer.
    /// </summary>
    private void ApplyToRenderer(SkinnedMeshRenderer renderer, SkinDataSO skin)
    {
        if (renderer == null) return;

        renderer.sharedMesh = skin.outfitMesh;

        if (skin.outfitMaterials != null && skin.outfitMaterials.Length > 0)
            renderer.sharedMaterials = skin.outfitMaterials;
    }

    #endregion
}

[System.Serializable]
public class CharacterVisualSet
{
    [Header("Character Type")]
    public CharacterType characterType;

    [Header("In-Game Renderers")]
    public SkinnedMeshRenderer gameShirtRenderer;
    public SkinnedMeshRenderer gamePantRenderer;
    public SkinnedMeshRenderer gameShoesRenderer;

    [Header("Preview Renderers")]
    public SkinnedMeshRenderer previewShirtRenderer;
    public SkinnedMeshRenderer previewPantRenderer;
    public SkinnedMeshRenderer previewShoesRenderer;
}
