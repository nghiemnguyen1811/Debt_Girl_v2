using UnityEngine;

[CreateAssetMenu(fileName = "New Character Tab", menuName = "Decorations/Character Tab")]
public class CharacterProfileSO : ScriptableObject
{
    [Header("Basic Info")]
    public CharacterType character;
    public string characterName;

    [Header("UI Sprites")]
    public Sprite avatarIcon;    // Small icon (e.g., for tabs)
    public Sprite portrait;      // Dialogue portrait or full-face image
}
