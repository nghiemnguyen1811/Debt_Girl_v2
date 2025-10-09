using UnityEngine;

[System.Serializable]
public class NPCDialogueEntry
{
    // ─────────────────────────────────────────────────────
    // Dialogue Entry Data
    // ─────────────────────────────────────────────────────
    public CharacterType characterType;
    public DialogueSequence dialogueSequence;
}

[CreateAssetMenu(fileName = "NPCDialogueData", menuName = "Dialogue/NPC Dialogue Data")]
public class NPCDialogueData : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // NPC Dialogue Data
    // ─────────────────────────────────────────────────────
    public string npcName;
    public NPCDialogueEntry[] dialogues;
}
