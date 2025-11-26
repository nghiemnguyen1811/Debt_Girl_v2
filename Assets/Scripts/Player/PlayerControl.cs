using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerAnimation))]
[RequireComponent(typeof(PlayerInteractDetector))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerStatsUI))]
[RequireComponent(typeof(MoodVisualizer))]
[RequireComponent(typeof(HeldPropSwitcher))]
[RequireComponent(typeof(PlayerOutfitVisualizer))]
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
    public HeldPropSwitcher propSwitcher { get; private set; }
    public PlayerOutfitVisualizer outfitVisualizer { get; private set; }

    #endregion

    #region === Character Profile ===

    [Header("Character Profile")]
    [SerializeField] private CharacterInfoSO characterProfile;

    /// <summary>
    /// Current character profile assigned to the player.
    /// </summary>
    public CharacterInfoSO CharacterProfile => characterProfile;

    /// <summary>
    /// Event fired whenever the character profile changes.
    /// </summary>
    public event Action<CharacterInfoSO> OnCharacterProfileChanged;

    #endregion

    #region === Unity Lifecycle ===

    protected override void Awake()
    {
        base.Awake();

        // Cache subsystem references
        inputHandler = GetComponent<PlayerInputHandler>();
        animationHandler = GetComponent<PlayerAnimation>();
        interactDetector = GetComponent<PlayerInteractDetector>();
        movementHandler = GetComponent<PlayerMovement>();
        stats = GetComponent<PlayerStats>();
        statsUI = GetComponent<PlayerStatsUI>();
        visualizer = GetComponent<MoodVisualizer>();
        propSwitcher = GetComponent<HeldPropSwitcher>();
        outfitVisualizer = GetComponent<PlayerOutfitVisualizer>();

        // Link private 'playerStats' field in PlayerStatsUI via reflection
        if (statsUI != null && stats != null)
        {
            statsUI.GetType()
                   .GetField("playerStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                   ?.SetValue(statsUI, stats);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return null;
        OnCharacterProfileChanged?.Invoke(characterProfile);
    }

    #endregion

    #region === Public API ===

    /// <summary>
    /// Sets a new CharacterProfile for the player.
    /// Invokes the OnCharacterProfileChanged event if the profile is updated.
    /// </summary>
    public void SetCharacterProfile(CharacterInfoSO newProfile)
    {
        if (newProfile == characterProfile) return;

        characterProfile = newProfile;

        // Notify subscribers
        OnCharacterProfileChanged?.Invoke(characterProfile);
    }

    #endregion
}
