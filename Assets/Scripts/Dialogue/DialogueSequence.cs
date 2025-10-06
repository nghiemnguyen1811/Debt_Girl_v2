using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public CharacterData speaker;
    [TextArea] public string text;
}

[CreateAssetMenu(fileName = "DialogueSequence", menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] lines;
}
