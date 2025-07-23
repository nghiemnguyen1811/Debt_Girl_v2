using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerAnimation))]
[RequireComponent(typeof(PlayerInteractDetector))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerStatsUI))]
[RequireComponent(typeof(MoodVisualizer))]
public class PlayerControl : SingletonMonobehaviour<PlayerControl>
{
    #region === Subsystem References ===

    public PlayerInputHandler inputHandler { get; private set; }
    public PlayerAnimation animationHandler { get; private set; }
    public PlayerInteractDetector interactDetector { get; private set; }
    public PlayerMovement movementHandler { get; private set; }
    public PlayerStats stats { get; private set; }
    public PlayerStatsUI statsUI { get; private set; }
    public MoodVisualizer visualizer { get; private set; }

    #endregion

    #region === Unity Lifecycle ===

    // Assign all required component references
    protected override void Awake()
    {
        base.Awake();

        inputHandler = GetComponent<PlayerInputHandler>();
        animationHandler = GetComponent<PlayerAnimation>();
        interactDetector = GetComponent<PlayerInteractDetector>();
        movementHandler = GetComponent<PlayerMovement>();
        stats = GetComponent<PlayerStats>();
        statsUI = GetComponent<PlayerStatsUI>();
        visualizer = GetComponent<MoodVisualizer>();

        // Manually link private 'playerStats' field in statsUI using reflection
        if (statsUI != null && stats != null)
        {
            statsUI.GetType()
                   .GetField("playerStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                   ?.SetValue(statsUI, stats);
        }
    }

    #endregion
}
