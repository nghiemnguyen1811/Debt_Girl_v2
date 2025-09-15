using System.Collections.Generic;
using UnityEngine;

public class DecorationManager : SingletonMonobehaviour<DecorationManager>
{
    [Header("Registered Decoration Items in Scene")]
    [SerializeField] private List<DecorationItem> allDecorations = new();

    public void RegisterDecoration(DecorationItem item)
    {
        if (!allDecorations.Contains(item))
            allDecorations.Add(item);
    }

    public void UnregisterDecoration(DecorationItem item)
    {
        if (allDecorations.Contains(item))
            allDecorations.Remove(item);
    }

    /// <summary>
    /// Activate the decoration GameObject if it matches ID and CharacterType
    /// </summary>
    public void UnlockDecoration(int id, CharacterType owner)
    {
        foreach (var decor in allDecorations)
        {
            if (decor.ItemID == id && decor.Owner == owner)
            {
                decor.SetActive(true);
                Debug.Log($"Decoration unlocked: {decor.ItemID} ({decor.Owner})");
            }
        }
    }
}
