using UnityEngine;

/// <summary>
/// Represents a Bed interactable object. 
/// Triggers rest logic and handles cooldowns to prevent spamming.
/// </summary>
public class Bed : InteractableBase, ICooldownInteractable
{
    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("Cooldown Settings")]
    [SerializeField] private float interactCooldown = 30f;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Internal State ===

    private float remainingCooldown;
    private bool isCoolingDown;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Events ===

    protected override void Start()
    {
        base.Start();
        ImportSaveData(SaveManager.LoadGame());
    }

    private void Update()
    {
        if (!isCoolingDown) return;

        remainingCooldown -= Time.deltaTime;

        if (remainingCooldown <= 0f)
        {
            remainingCooldown = 0f;
            isCoolingDown = false;
        }
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Interaction Logic ===

    /// <summary>
    /// Triggered when the player attempts to use the bed.
    /// Checks cooldown before allowing the base interaction.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        // Check Cooldown
        if (IsOnCooldown(out float remaining))
        {
            ShowCooldownWarning(remaining);
            return;
        }

        // Start Cooldown & Logic
        remainingCooldown = interactCooldown;
        isCoolingDown = true;
        AutoSave();

        base.OnInteract(showProp);
    }

    public override void OnStopInteract() => base.OnStopInteract();

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === ICooldownInteractable Implementation ===

    public bool IsOnCooldown(out float remainingTime)
    {
        remainingTime = Mathf.Max(0f, remainingCooldown);
        return isCoolingDown && remainingCooldown > 0f;
    }

    public void ShowCooldownWarning(float remainingTime)
    {
        // Delegate warning display to GameManager
        GameManager.Instance.ShowCooldownWarning();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Save & Load ===

    private void OnApplicationQuit() => AutoSave();

    public void ImportSaveData(SaveData data)
    {
        if (data == null) return;
        remainingCooldown = data.remainingBedCooldown;
        isCoolingDown = remainingCooldown > 0f;
    }

    public void AutoSave()
    {
        if (SaveManager.Data == null) return;
        SaveManager.Data.remainingBedCooldown = remainingCooldown;
        SaveManager.SaveGame();
    }

    #endregion
}

/// <summary>
/// Interface for interactables that have a cooldown period.
/// </summary>
public interface ICooldownInteractable
{
    bool IsOnCooldown(out float remainingTime);
    void ShowCooldownWarning(float remainingTime);
}