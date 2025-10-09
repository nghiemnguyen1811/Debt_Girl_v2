using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class DialogueLine
{
    // ─────────────────────────────────────────────────────
    // Dialogue Line Data
    // ─────────────────────────────────────────────────────
    [LabelText("Speaker Type")]
    public CharacterType speakerType;

    [LabelText("Speaker Portraits")]
    public SpeakerPortraits portraits;

    [LabelText("Dialogue Text"), TextArea]
    public string dialogueText;
}

[System.Serializable]
public class SpeakerPortraits
{
    [LabelText("Player Portrait")]
    public Sprite playerPortrait;

    [LabelText("NPC Portrait")]
    public Sprite npcPortrait;
}

[CreateAssetMenu(fileName = "DialogueSequence", menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    // ─────────────────────────────────────────────────────
    // Sequence Data
    // ─────────────────────────────────────────────────────
    [LabelText("Dialogue Lines")]
    public DialogueLine[] lines;
}
