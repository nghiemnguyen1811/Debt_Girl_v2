using UnityEngine;

[CreateAssetMenu(fileName = "New Character Info", menuName = "Characters/Character Info")]
public class CharacterInfoSO : ScriptableObject
{
    [Header("Basic Info")]
    public CharacterType characterType;             // Logical type used for matching/dialogue
    public string characterName;
    public Sprite avatarIcon;                       // Small icon (tabs, lists, selection)
}
