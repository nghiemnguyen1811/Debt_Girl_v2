using UnityEngine;

[CreateAssetMenu(fileName = "NewPostData", menuName = "Social/Post Data")]
public class PostDataSO : ScriptableObject
{
    [TextArea]
    public string caption;

    public string captionId;

    public Sprite image;
}
