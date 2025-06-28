using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerAnimation))]
[RequireComponent(typeof(PlayerInteractDetector))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerStatsUI))]
[RequireComponent(typeof(MoodVisualizer))]
public class PlayerControl : MonoBehaviour
{
    public PlayerInputHandler inputHandler { get; private set; }
    public PlayerAnimation animationHandler { get; private set; }
    public PlayerInteractDetector interactDetector { get; private set; }
    public PlayerMovement movementHandler { get; private set; }
    public PlayerStats stats { get; private set; }
    public PlayerStatsUI statsUI { get; private set; }
    public MoodVisualizer visualizer { get; private set; }

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        animationHandler = GetComponent<PlayerAnimation>();
        interactDetector = GetComponent<PlayerInteractDetector>();
        movementHandler = GetComponent<PlayerMovement>();
        stats = GetComponent<PlayerStats>();
        statsUI = GetComponent<PlayerStatsUI>();
        visualizer = GetComponent<MoodVisualizer>();

        if (statsUI != null && stats != null)
            statsUI.GetType().GetField("playerStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                   ?.SetValue(statsUI, stats);
    }

    private void Start()
    {
        AudioManager.Instance.PlayMusic(0);
    }
}
