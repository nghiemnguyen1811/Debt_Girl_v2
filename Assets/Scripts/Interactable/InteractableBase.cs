using UnityEngine;
using EPOOutline;

/// <summary>
/// Base class for all interactable objects. 
/// Provides shared data and behavior like outline, particles, and common properties.
/// </summary>
public abstract class InteractableBase : MonoBehaviour
{
    #region === Serialized Fields ===

    [Header("Elements")]
    [SerializeField] protected Transform interactPoint;

    [Header("Interactable Data")]
    [SerializeField] protected InteractableDataSO data;

    [Header("Mood Icon Offset")]
    [SerializeField] protected Vector3 moodIconOffset;

    [Header("Visual Effect")]
    [SerializeField] protected GameObject interactParticle;

    [Header("Sound")]
    [SerializeField] protected int soundId = -1;

    #endregion

    #region === Unity Events ===

    protected virtual void Start()
    {
        SetOutline(false);
        SetParticle(false);
    }

    #endregion

    #region === Properties ===

    /// <summary>
    /// Optional outline visual for highlighting.
    /// </summary>
    public Outlinable Outlinable => GetComponent<Outlinable>();

    /// <summary>
    /// Returns the assigned data asset (name, animation, duration).
    /// </summary>
    public virtual InteractableDataSO Data => data;

    /// <summary>
    /// Returns the energy amount provided by this interactable.
    /// </summary>
    public virtual float GetEnergyAmount() => data != null ? data.energyAmount : 0f;

    /// <summary>
    /// Returns the point used to align the player when interacting.
    /// </summary>
    public virtual Transform GetInteractPoint() => interactPoint;

    /// <summary>
    /// Position offset for UI mood icons.
    /// </summary>
    public virtual Vector3 MoodIconOffset => moodIconOffset;

    /// <summary>
    /// The particle to play during interaction.
    /// </summary>
    public virtual GameObject InteractParticle => interactParticle;

    /// <summary>
    /// Returns the sound ID associated with this interaction.
    /// </summary>
    public virtual int SoundId => soundId;

    #endregion

    #region === Interaction Info ===

    /// <summary>
    /// Object name used for debug or UI display.
    /// </summary>
    public virtual string GetObjectName() => data != null ? data.objectName : "Unknown";

    /// <summary>
    /// The animation clip name tied to this interaction.
    /// </summary>
    public virtual string GetAnimationName() => data != null ? data.animationName : string.Empty;

    /// <summary>
    /// Duration in seconds for how long the interaction takes.
    /// </summary>
    public virtual float GetDuration() => data != null ? data.interactionDuration : 0f;

    /// <summary>
    /// Whether this interaction plays animation immediately or waits for user action (like UI selection).
    /// </summary>
    public virtual bool ShouldPlayAnimationImmediately() => data != null && data.playAnimationImmediately;

    #endregion

    #region === Interaction Events ===

    /// <summary>
    /// Called when the player enters interaction range.
    /// </summary>
    public virtual void OnEnter() => SetOutline(true);

    /// <summary>
    /// Called when the player exits interaction range.
    /// </summary>
    public virtual void OnExit() => SetOutline(false);

    /// <summary>
    /// Called when interaction begins.
    /// Implement this in derived classes.
    /// </summary>
    public abstract void OnInteract(bool playSound = true);

    /// <summary>
    /// Called when interaction stops.
    /// Implement this in derived classes.
    /// </summary>
    public abstract void OnStopInteract();

    #endregion

    #region === Visual / Audio Tools ===

    /// <summary>
    /// Toggle the outline component on or off.
    /// </summary>
    protected void SetOutline(bool enabled)
    {
        if (Outlinable != null)
            Outlinable.enabled = enabled;
    }

    /// <summary>
    /// Toggle the interaction particle on or off.
    /// </summary>
    protected void SetParticle(bool enabled)
    {
        if (interactParticle != null && interactParticle.activeSelf != enabled)
            interactParticle.SetActive(enabled);
    }

    /// <summary>
    /// Play or stop the assigned sound based on interaction state.
    /// </summary>
    /// <param name="play">True to play the sound; false to stop it.</param>
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

    #endregion
}
