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

    [BoxGroup("Options"), LabelText("Use Interaction Prop")]
    public bool useProp;

    [BoxGroup("Options"), LabelText("Use Sound")]
    public bool useSound;

    // ─────────────────────────────────────────────────────
    // Serialized Fields (Grouped)
    // ─────────────────────────────────────────────────────
    [BoxGroup("Core"), LabelText("Interaction Point")]
    [SerializeField] protected Transform interactPoint;

    [BoxGroup("Core"), LabelText("Allowed Character")]
    [SerializeField] protected CharacterType allowedCharacter = CharacterType.All;

    [BoxGroup("Core"), LabelText("Interaction Mode")]
    [SerializeField] protected InteractionPlayMode interactionMode;

    [BoxGroup("Data"), ShowIf("useData")]
    [SerializeField] protected InteractableDataSO data;

    [BoxGroup("Visuals"), ShowIf("useMoodOffset")]
    [SerializeField] protected Vector3 moodIconOffset;

    [BoxGroup("Visuals"), ShowIf("useParticle")]
    [SerializeField] protected GameObject interactParticle;

    [BoxGroup("Visuals"), LabelText("Interaction Prop"), ShowIf("useProp")]
    [SerializeField] protected InteractionPropType interactionProp;

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

    public Outlinable Outlinable => GetComponent<Outlinable>();

    public virtual InteractableDataSO Data => data;

    public virtual float GetEnergyAmount() => data != null ? data.energyAmount : 0f;

    public virtual Transform GetInteractPoint() => interactPoint;

    public virtual Vector3 MoodIconOffset => moodIconOffset;

    public virtual GameObject InteractParticle => interactParticle;

    public virtual CharacterType AllowedCharacter => allowedCharacter;

    public virtual int SoundId => soundId;

    // ─────────────────────────────────────────────────────
    // Interaction Info (for UI or animation systems)
    // ─────────────────────────────────────────────────────

    public virtual string GetObjectName() => data != null ? data.objectName : "Unknown";

    public virtual string GetAnimationName() => data != null ? data.animationName : string.Empty;

    public virtual float GetDuration() => data != null ? data.interactionDuration : 0f;

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
    public virtual void OnInteract(bool showProp = true)
    {
        SetOutline(false);
        SetParticle(true);
        SetInteractionPropVisible(showProp);
        HandleSound(showProp);
    }

    /// <summary>
    /// Called when interaction ends or is cancelled.
    /// Must be implemented in subclasses.
    /// </summary>
    public virtual void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);
        SetInteractionPropVisible(false);
        HandleSound(play: false);
    }

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
    /// Enables or disables the interaction prop (e.g. broom, pan).
    /// </summary>
    /// <param name="show">True = show, False = hide</param>
    protected void SetInteractionPropVisible(bool show)
    {
        if (useProp && interactionProp != InteractionPropType.None)
            PlayerControl.Instance.propSwitcher.SetPropActiveByType(interactionProp, show);
    }


    /// <summary>
    /// Plays or stops the assigned sound via AudioManager.
    /// </summary>
    /// <param name="play">True = play, False = stop</param>
    protected void HandleSound(bool play)
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
