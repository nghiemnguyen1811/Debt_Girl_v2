using Sirenix.OdinInspector;
using UnityEngine;

public class NPC : InteractableBase
{
    // ─────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [BoxGroup("Dialogue")]
    [SerializeField] private NPCDialogueData npcData;

    #endregion

    // ─────────────────────────────────────────────────────
    #region === Interaction Events ===

    /// <summary>
    /// Called when the player starts interacting with the NPC.
    /// Disables outline, enables particles, and plays sound.
    /// </summary>
    public override void OnInteract(bool showProp = true)
    {
        Debug.Log($"Interacted with: {GetObjectName()}");
        base.OnInteract(showProp);

        DialogueManager.Instance.StartNpcDialogue(npcData);
    }

    #endregion
}
