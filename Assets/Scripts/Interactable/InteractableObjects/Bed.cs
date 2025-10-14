using System;
using UnityEngine;
using URandom = UnityEngine.Random;

/// <summary>
/// Bed interactable — triggers sound and VFX when interacted with.
/// </summary>
public class Bed : InteractableBase, ICooldownInteractable
{
    [Header("Cooldown Settings")]
    [SerializeField] private float interactCooldown = 30f;

    private float remainingCooldown;
    private bool isCoolingDown;

    [Header("Cooldown Warning Messages")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] cooldownWarningMessages = {
        "아직은 쉴 때가 아니에요.",
        "조금만 기다렸다가 다시 쉬어요.",
        "너무 빨라요, 잠시 후에 다시 시도해요.",
        "조금 더 쉬었다가 다시 누워요.",
        "아직 준비 중이에요. 잠깐만요!"
    };

    #region === Interaction Events ===

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
            Debug.Log("[Bed] Cooldown finished!");
        }
    }

    /// <summary>
    /// Called when the player starts interacting with the bed.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        if (IsOnCooldown(out float remaining))
        {
            ShowCooldownWarning(remaining);
            return;
        }

        remainingCooldown = interactCooldown;
        isCoolingDown = true;

        AutoSave();

        base.OnInteract(showProp);
    }

    /// <summary>
    /// Called when the player stops interacting with the bed.
    /// Re-enables outline, disables particles, and stops sound.
    /// </summary>
    public override void OnStopInteract() => base.OnStopInteract();

    private void OnApplicationQuit()
    {
        AutoSave();
    }

    #endregion

    #region === ICooldownInteractable Implementation ===

    public bool IsOnCooldown(out float remainingTime)
    {
        remainingTime = Mathf.Max(0f, remainingCooldown);
        return isCoolingDown && remainingCooldown > 0f;
    }

    public void ShowCooldownWarning(float remainingTime)
    {
        string warning = cooldownWarningMessages[URandom.Range(0, cooldownWarningMessages.Length)];
        UIManager.Instance.ShowWarningText(warning);
    }

    #endregion

    //─────────────────────────────────────────────────────
    // SAVE / LOAD API
    //─────────────────────────────────────────────────────
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

}

public interface ICooldownInteractable
{
    bool IsOnCooldown(out float remainingTime);
    void ShowCooldownWarning(float remainingTime);
}