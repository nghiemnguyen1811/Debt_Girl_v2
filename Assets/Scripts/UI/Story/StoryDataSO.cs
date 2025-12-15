using UnityEngine;

[CreateAssetMenu(fileName = "StoryData", menuName = "Story/Story Data")]
public class StoryDataSO : ScriptableObject
{
    [Header("Story Visual")]
    public Sprite illustration;

    //[Header("Story Content")]
    //[TextArea(4, 12)]
    //public string storyText;

    public string storyTextKey;
}
