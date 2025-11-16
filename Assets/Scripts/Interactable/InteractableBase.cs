using UnityEngine;
using EPOOutline;
using Sirenix.OdinInspector;
using System;
using System.Collections;

/// <summary>
/// Base class for all interactable objects.
/// Provides shared behavior such as outline control, particles,
/// sound handling, and quest event emission.
/// </summary>
public abstract class InteractableBase : MonoBehaviour
{
    #region Events

    /// <summary>
    /// Triggered when the interaction ends.  
    /// Used for daily quest progression.
    /// </summary>
    public event Action<DailyQuestType, DailyActivity> OnStopInteractable;

    #endregion

    #region Inspector Toggles

    [BoxGroup("Options"), LabelText("Use Data")] public bool useData;
    [BoxGroup("Options"), LabelText("Use Mood Offset")] public bool useMoodOffset;
    [BoxGroup("Options"), LabelText("Use Particle")] public bool useParticle;
    [BoxGroup("Options"), LabelText("Use Interaction Prop")] public bool useProp;
    [BoxGroup("Options"), LabelText("Use Sound")] public bool useSound;

    #endregion

    #region Serialized Fields

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

    #endregion

    #region Unity Lifecycle

    protected virtual void Start()
    {
        SetOutline(false);
        SetParticle(false);
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterAfterFrame());
    }

    private void OnDisable()
    {
        if (DailyQuestManager.Instance != null)
            DailyQuestManager.Instance.UnregisterInteractable(this);
    }

    /// <summary>
    /// Registers this interactable with the DailyQuestManager
    /// one frame later to ensure initialization order.
    /// </summary>
    private IEnumerator RegisterAfterFrame()
    {
        yield return null;

        if (DailyQuestManager.Instance != null)
            DailyQuestManager.Instance.RegisterInteractable(this);
    }

    #endregion

    #region Properties

    public Outlinable Outlinable => GetComponent<Outlinable>();
    public virtual InteractableDataSO Data => data;
    public virtual float GetEnergyAmount() => data != null ? data.energyAmount : 0f;
    public virtual Transform GetInteractPoint() => interactPoint;
    public virtual Vector3 MoodIconOffset => moodIconOffset;
    public virtual GameObject InteractParticle => interactParticle;
    public virtual CharacterType AllowedCharacter => allowedCharacter;
    public virtual int SoundId => soundId;

    #endregion

    #region Interaction Info

    /// <summary>Returns the display name of this interactable.</summary>
    public virtual string GetObjectName() =>
        data != null ? data.objectName : "Unknown";

    /// <summary>Animation name to play during interaction.</summary>
    public virtual string GetAnimationName() =>
        data != null ? data.animationName : string.Empty;

    /// <summary>Interaction duration in seconds.</summary>
    public virtual float GetDuration() =>
        data != null ? data.interactionDuration : 0f;

    /// <summary>Returns the interaction mode used for animations.</summary>
    public virtual InteractionPlayMode GetInteractionMode() => interactionMode;

    #endregion

    #region Interaction Lifecycle

    /// <summary>Called when the player enters interaction range.</summary>
    public virtual void OnEnter() => SetOutline(true);

    /// <summary>Called when the player leaves interaction range.</summary>
    public virtual void OnExit() => SetOutline(false);

    /// <summary>Called when interaction begins.</summary>
    public virtual void OnInteract(bool showProp = true)
    {
        SetOutline(false);
        SetParticle(true);
        SetInteractionPropVisible(showProp);
        HandleSound(showProp);
    }

    /// <summary>Called when interaction ends or is canceled.</summary>
    public virtual void OnStopInteract()
    {
        SetOutline(true);
        SetParticle(false);
        SetInteractionPropVisible(false);
        HandleSound(false);

        OnStopInteractable?.Invoke(DailyQuestType.Interact, dailyActivity);
    }

    #endregion

    #region Visual & Audio Helpers

    /// <summary>Enables or disables outline.</summary>
    protected void SetOutline(bool enabled)
    {
        if (Outlinable != null)
            Outlinable.enabled = enabled;
    }

    /// <summary>Enables or disables particle effects.</summary>
    protected void SetParticle(bool enabled)
    {
        if (interactParticle != null && interactParticle.activeSelf != enabled)
            interactParticle.SetActive(enabled);
    }

    /// <summary>Shows or hides the interaction prop.</summary>
    protected void SetInteractionPropVisible(bool show)
    {
        if (useProp && interactionProp != InteractionPropType.None)
            PlayerControl.Instance.propSwitcher.SetPropActiveByType(interactionProp, show);
    }

    /// <summary>Plays or stops interaction sound.</summary>
    protected void HandleSound(bool play)
    {
        if (SoundId <= -1)
            return;

        if (play)
            AudioManager.Instance.PlayInteractSound(SoundId);
        else
            AudioManager.Instance.StopSound(SoundId);
    }

    #endregion
}
