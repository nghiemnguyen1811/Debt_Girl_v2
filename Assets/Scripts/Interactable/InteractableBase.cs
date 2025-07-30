using UnityEngine;
using EPOOutline;
using Sirenix.OdinInspector;

/// <summary>
/// Base class for all interactable objects. 
/// Provides shared data and behavior like outline, particles, and interaction logic.
/// </summary>
public abstract class InteractableBase : MonoBehaviour
{
    // ─────────────────────────────────────────────────────
    // Toggle Options for Odin (Inspector cleaner)
    // ─────────────────────────────────────────────────────
    [BoxGroup("Options"), LabelText("Use Data")]
    public bool useData;

    [BoxGroup("Options"), LabelText("Use Mood Offset")]
    public bool useMoodOffset;

    [BoxGroup("Options"), LabelText("Use Particle")]
    public bool useParticle;

    [BoxGroup("Options"), LabelText("Use Sound")]
    public bool useSound;

    // ─────────────────────────────────────────────────────
    // Serialized Fields (Grouped)
    // ─────────────────────────────────────────────────────
    [BoxGroup("Core"), LabelText("Interaction Point")]
    [SerializeField] protected Transform interactPoint;

    [BoxGroup("Core"), LabelText("Interaction Mode")]
    [SerializeField] protected InteractionPlayMode interactionMode;

    [BoxGroup("Data"), ShowIf("useData")]
    [SerializeField] protected InteractableDataSO data;

    [BoxGroup("Visuals"), ShowIf("useMoodOffset")]
    [SerializeField] protected Vector3 moodIconOffset;

    [BoxGroup("Visuals"), ShowIf("useParticle")]
    [SerializeField] protected GameObject interactParticle;

    [BoxGroup("Audio"), ShowIf("useSound")]
    [SerializeField] protected int soundId = -1;

    // ─────────────────────────────────────────────────────
    // Unity Events
    // ─────────────────────────────────────────────────────
    protected virtual void Start()
    {
        SetOutline(false);
        SetParticle(false);
    }

    // ─────────────────────────────────────────────────────
    // Properties
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Cached reference to Outlinable component (used for highlighting).
    /// </summary>
    public Outlinable Outlinable => GetComponent<Outlinable>();

    /// <summary>
    /// The InteractableData scriptable object assigned to this object.
    /// </summary>
    public virtual InteractableDataSO Data => data;

    /// <summary>
    /// Returns energy value from the data asset.
    /// </summary>
    public virtual float GetEnergyAmount() => data != null ? data.energyAmount : 0f;

    /// <summary>
    /// Transform point where the player should align to interact.
    /// </summary>
    public virtual Transform GetInteractPoint() => interactPoint;

    /// <summary>
    /// World offset used for UI mood icon placement.
    /// </summary>
    public virtual Vector3 MoodIconOffset => moodIconOffset;

    /// <summary>
    /// Particle system played when interaction happens.
    /// </summary>
    public virtual GameObject InteractParticle => interactParticle;

    /// <summary>
    /// ID used to fetch sound from AudioManager.
    /// </summary>
    public virtual int SoundId => soundId;

    // ─────────────────────────────────────────────────────
    // Interaction Info (for UI or animation systems)
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Name of this object, for UI display or debug.
    /// </summary>
    public virtual string GetObjectName() => data != null ? data.objectName : "Unknown";

    /// <summary>
    /// Animation name (if any) to be played during interaction.
    /// </summary>
    public virtual string GetAnimationName() => data != null ? data.animationName : string.Empty;

    /// <summary>
    /// Time duration of the interaction in seconds.
    /// </summary>
    public virtual float GetDuration() => data != null ? data.interactionDuration : 0f;

    /// <summary>
    /// Gets how this object should handle interaction (instant, confirm, sound only).
    /// </summary>
    public virtual InteractionPlayMode GetInteractionMode() => interactionMode;

    // ─────────────────────────────────────────────────────
    // Interaction Lifecycle (override in subclasses)
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Called when player enters interaction range.
    /// Usually enables highlight or UI prompt.
    /// </summary>
    public virtual void OnEnter() => SetOutline(true);

    /// <summary>
    /// Called when player exits interaction range.
    /// Usually disables highlight or UI prompt.
    /// </summary>
    public virtual void OnExit() => SetOutline(false);

    /// <summary>
    /// Called when player initiates interaction.
    /// Must be implemented in subclasses.
    /// </summary>
    public abstract void OnInteract(bool playSound = true);

    /// <summary>
    /// Called when interaction ends or is cancelled.
    /// Must be implemented in subclasses.
    /// </summary>
    public abstract void OnStopInteract();

    // ─────────────────────────────────────────────────────
    // Visual & Audio Helpers
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Toggles outline component on or off.
    /// </summary>
    protected void SetOutline(bool enabled)
    {
        if (Outlinable != null)
            Outlinable.enabled = enabled;
    }

    /// <summary>
    /// Enables/disables interaction particle visual effect.
    /// </summary>
    protected void SetParticle(bool enabled)
    {
        if (interactParticle != null && interactParticle.activeSelf != enabled)
            interactParticle.SetActive(enabled);
    }

    /// <summary>
    /// Plays or stops the assigned sound via AudioManager.
    /// </summary>
    /// <param name="play">True = play, False = stop</param>
    public void HandleSound(bool play)
    {
        if (SoundId <= -1)
        {
            Debug.Log("No sound available to play.");
            return;
        }

        if (play) AudioManager.Instance.PlayInteractSound(SoundId);
        else AudioManager.Instance.StopSound(SoundId);
    }
}
