using UnityEngine;
using System;

/// <summary>
/// GameManager is the global controller for player progress and core game state.
/// It manages player level, triggers level change events, and starts background music.
/// </summary>
public class GameManager : SingletonMonobehaviour<GameManager>, ILoadable
{
    // ─────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Event fired whenever the player's level changes.
    /// </summary>
    public event Action OnLevelChanged;

    // ─────────────────────────────────────────────────────
    // Player Progress
    // ─────────────────────────────────────────────────────
    [Header("Player Progress")]
    [SerializeField, Min(1)]
    private int currentLevel = 1;

    /// <summary>
    /// Current level of the player (minimum = 1).
    /// </summary>
    public int CurrentLevel => currentLevel;

    // ─────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────
    private void Start()
    {
        // Start background music
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(1);

        SetLevel(1);
    }

    // ─────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Increases the current level by 1 and notifies listeners.
    /// </summary>
    public void IncreaseLevel()
    {
        currentLevel++;
        NotifyLevelChanged();
        AutoSave();
    }

    /// <summary>
    /// Sets the current level to a specific value (minimum = 1) and notifies listeners.
    /// </summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        NotifyLevelChanged();
        AutoSave();
    }

    // ─────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Fires the OnLevelChanged event.
    /// </summary>
    private void NotifyLevelChanged()
    {
        OnLevelChanged?.Invoke();
        Debug.Log($"[GameManager] Player level updated: {currentLevel}");
    }

    // ─────────────────────────────────────────────────────
    // Save/Load API
    // ─────────────────────────────────────────────────────
    protected void AutoSave()
    {
        SaveManager.Data.playerLevel = currentLevel;
        SaveManager.SaveGame();
    }

    public void ImportSaveData(SaveData saveData)
    {
        SetLevel(saveData.playerLevel);
    }
}
