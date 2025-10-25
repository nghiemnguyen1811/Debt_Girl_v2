using UnityEngine;
using EPOOutline;
using Sirenix.OdinInspector;
using System;
using System.Collections;

/// <summary>
/// Base class for all interactable objects. 
/// Provides shared data and behavior such as outline, particles, sounds, and quest linking.
/// </summary>
public abstract class InteractableBase : MonoBehaviour
{
    // ==================================================
    // ▶ EVENTS
    // ==================================================
    /// <summary>Triggered when the interaction ends — used for daily quest tracking.</summary>
    public event Action<DailyQuestType, DailyActivity> OnStopInteractable;

    // ==================================================
    // ▶ INSPECTOR TOGGLES (for Odin organization)
    // ==================================================
    [BoxGroup("Options"), LabelText("Use Data")] public bool useData;
    [BoxGroup("Options"), LabelText("Use Mood Offset")] public bool useMoodOffset;
    [BoxGroup("Options"), LabelText("Use Particle")] public bool useParticle;
    [BoxGroup("Options"), LabelText("Use Interaction Prop")] public bool useProp;
    [BoxGroup("Options"), LabelText("Use Sound")] public bool useSound;

    // ==================================================
    // ▶ SERIALIZED FIELDS
    // ==================================================
    [BoxGroup("Core"), LabelText("Interaction Point")]
    [SerializeField] protected Transform interactPoint;

    [BoxGroup("Core"), LabelText("Allowed Character")]
    [SerializeField] protected CharacterType allowedCharacter = CharacterType.All;

    [BoxGroup("Core"), LabelText("Interaction Mode")]
    [SerializeField] protected InteractionPlayMode interactionMode;

    [BoxGroup("Mission"), LabelText("Daily Activity")]
    [SerializeField] protected DailyActivity dailyActivity;

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

    // ==================================================
    // ▶ UNITY LIFECYCLE
    // ==================================================
    protected virtual void Start()
    {
        SetOutline(false);
        SetParticle(false);
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterAfterFrame());
    }

    /// <summary>
    /// Waits one frame before registering with the DailyQuestManager
    /// to ensure the manager's Instance is initialized.
    /// </summary>
    private IEnumerator RegisterAfterFrame()
    {
        yield return null;
        if (DailyQuestManager.Instance != null)
            DailyQuestManager.Instance.RegisterInteractable(this);
    }

    private void OnDisable()
    {
        if (DailyQuestManager.Instance != null)
            DailyQuestManager.Instance.UnregisterInteractable(this);
    }

    // ==================================================
    // ▶ PROPERTIES
    // ==================================================
    public Outlinable Outlinable => GetComponent<Outlinable>();
    public virtual InteractableDataSO Data => data;
    public virtual float GetEnergyAmount() => data != null ? data.energyAmount : 0f;
    public virtual Transform GetInteractPoint() => interactPoint;
    public virtual Vector3 MoodIconOffset => moodIconOffset;
    public virtual GameObject InteractParticle => interactParticle;
    public virtual CharacterType AllowedCharacter => allowedCharacter;
    public virtual int SoundId => soundId;

    // ==================================================
    // ▶ INTERACTION INFO
    // ==================================================
    /// <summary>Returns the display name of the interactable object.</summary>
    public virtual string GetObjectName() => data != null ? data.objectName : "Unknown";

    /// <summary>Returns the animation name used for interaction.</summary>
    public virtual string GetAnimationName() => data != null ? data.animationName : string.Empty;

    /// <summary>Returns the duration of interaction in seconds.</summary>
    public virtual float GetDuration() => data != null ? data.interactionDuration : 0f;

    /// <summary>Returns the interaction mode for animation or behavior.</summary>
    public virtual InteractionPlayMode GetInteractionMode() => interactionMode;

    // ==================================================
    // ▶ INTERACTION LIFECYCLE (override in subclasses)
    // ==================================================
    /// <summary>Called when the player enters the interaction range.</summary>
    public virtual void OnEnter() => SetOutline(true);

    /// <summary>Called when the player exits the interaction range.</summary>
    public virtual void OnExit() => SetOutline(false);

    /// <summary>Called when the player initiates interaction.</summary>
    public virtual void OnInteract(bool showProp = true)
    {
        SetOutline(false);
        SetParticle(true);
        SetInteractionPropVisible(showProp);
        HandleSound(showProp);
    }

    /// <summary>Called when the interaction ends or is canceled.</summary>
    public virtual void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);
        SetInteractionPropVisible(false);
        HandleSound(false);

        OnStopInteractable?.Invoke(DailyQuestType.Interact, dailyActivity);
    }

    // ==================================================
    // ▶ VISUAL & AUDIO HELPERS
    // ==================================================
    /// <summary>Enables or disables the outline effect.</summary>
    protected void SetOutline(bool enabled)
    {
        if (Outlinable != null)
            Outlinable.enabled = enabled;
    }

    /// <summary>Enables or disables the interaction particle.</summary>
    protected void SetParticle(bool enabled)
    {
        if (interactParticle != null && interactParticle.activeSelf != enabled)
            interactParticle.SetActive(enabled);
    }

    /// <summary>Shows or hides the player's interaction prop (e.g. broom, pan).</summary>
    protected void SetInteractionPropVisible(bool show)
    {
        if (useProp && interactionProp != InteractionPropType.None)
            PlayerControl.Instance.propSwitcher.SetPropActiveByType(interactionProp, show);
    }

    /// <summary>Handles playing or stopping sound effects.</summary>
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
