using UnityEngine;

/// <summary>
/// Manages global game state such as player level and starting music.
/// </summary>
public class GameManager : SingletonMonobehaviour<GameManager>
{
    #region === Player Progress ===

    [Header("Player Progress")]
    private int currentLevel = 1;

    /// <summary>
    /// Current level of the player (minimum = 1).
    /// </summary>
    public int CurrentLevel => currentLevel;

    #endregion

    #region === Unity Events ===

    // Play background music when the game starts
    private void Start()
    {
        AudioManager.Instance.PlayMusic(1);
    }

    #endregion

    #region === Public Methods ===

    /// <summary>
    /// Increases the current level by 1.
    /// </summary>
    public void IncreaseLevel()
    {
        currentLevel++;
    }

    /// <summary>
    /// Sets the current level to a specific value (minimum = 1).
    /// </summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
    }

    #endregion
}
