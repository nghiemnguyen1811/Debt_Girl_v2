using UnityEngine;

[CreateAssetMenu(fileName = "New Character Tab", menuName = "Decorations/Character Tab")]
public class CharacterTabSO : ScriptableObject
{
    [Header("Character Info")]
    public CharacterType character;

    [Header("UI")]
    public Sprite avatarIcon;
}
