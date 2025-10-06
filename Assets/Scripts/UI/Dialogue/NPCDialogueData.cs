using UnityEngine;

[System.Serializable]
public class NPCDialogueEntry
{
    public CharacterType characterType;    
    public DialogueSequence dialogueSequence;  
    public Sprite npcPortrait;                 
}

[CreateAssetMenu(fileName = "NPCDialogueData", menuName = "Dialogue/NPC Dialogue Data")]
public class NPCDialogueData : ScriptableObject
{
    public string npcName;
    public NPCDialogueEntry[] dialogues;
}
