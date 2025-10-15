using System.Collections.Generic;
using UnityEngine;

public class DecorationManager : SingletonMonobehaviour<DecorationManager>
{
    [Header("Registered Decoration Items in Scene")]
    [SerializeField] private List<DecorationItem> allDecorations = new();

    private readonly HashSet<(int id, CharacterType owner)> ownedDecorations = new();


    /// <summary>
    /// Registers a decoration to the manager when it spawns.
    /// </summary>
    public void RegisterDecoration(DecorationItem decor)
    {
        if (!allDecorations.Contains(decor))
            allDecorations.Add(decor);

        decor.SetActive(IsOwned(decor.ItemID, decor.Owner));
    }

    /// <summary>
    /// Unregisters a decoration when destroyed.
    /// </summary>
    public void UnregisterDecoration(DecorationItem item)
    {
        if (allDecorations.Contains(item))
            allDecorations.Remove(item);
    }

    // ─────────────────────────────────────────────────────
    // Unlock & Ownership
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Marks a decoration as unlocked and activates it in the scene.
    /// </summary>
    public void UnlockDecoration(int id, CharacterType owner)
    {
        if (ownedDecorations.Contains((id, owner))) return;

        ownedDecorations.Add((id, owner));

        foreach (var decor in allDecorations)
        {
            if (decor.ItemID == id && decor.Owner == owner)
                decor.SetActive(true);
        }

        AutoSave();
    }

    /// <summary>
    /// Checks if a decoration with a specific ID and Owner is owned.
    /// </summary>
    public bool IsOwned(int id, CharacterType owner)
    {
        return ownedDecorations.Contains((id, owner));
    }

    // ─────────────────────────────────────────────────────
    // Save & Load
    // ─────────────────────────────────────────────────────

    public void AutoSave()
    {
        if (SaveManager.Data == null) return;

        SaveManager.Data.ownedDecorations.Clear();

        foreach (var owned in ownedDecorations)
        {
            SaveManager.Data.ownedDecorations.Add(new OwnedDecorationEntry
            {
                id = owned.id,
                owner = owned.owner
            });
        }

        SaveManager.SaveGame();
        Debug.Log($"[DecorationManager] AutoSaved {ownedDecorations.Count} owned items");
    }

    public void ImportSaveData(SaveData data)
    {
        ownedDecorations.Clear();

        if (data?.ownedDecorations == null) return;

        foreach (var entry in data.ownedDecorations)
            ownedDecorations.Add((entry.id, entry.owner));

        foreach (var decor in allDecorations)
        {
            bool active = ownedDecorations.Contains((decor.ItemID, decor.Owner));
            decor.SetActive(active);
        }

        Debug.Log($"[DecorationManager] Imported {ownedDecorations.Count} decorations");
    }
}
