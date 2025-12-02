using UnityEngine;

/// <summary>
/// Data asset for a single guide entry (used by a guide tab).
/// </summary>
[CreateAssetMenu(fileName = "GuideCardData", menuName = "Tutorial/Guide Card Data", order = 0)]
public class GuideCardDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string systemName;

    [TextArea(2, 4)]
    public string description;

    [Header("Guide Images")]
    public Sprite[] guideImages;
}
