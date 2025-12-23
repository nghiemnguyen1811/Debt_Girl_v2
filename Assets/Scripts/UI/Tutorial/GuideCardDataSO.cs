using UnityEngine;

/// <summary>
/// ScriptableObject holding all data for one guide tab.
/// </summary>
[CreateAssetMenu(fileName = "GuideCardData", menuName = "Tutorial/Guide Card Data", order = 0)]
public class GuideCardDataSO : ScriptableObject
{
    #region === Basic Info ===

    [Header("Basic Info")]
    public string systemName;                // Name shown on the guide tab

    public string systemNameKey;

    //[TextArea(2, 4)]
    //public string description;

    public string descriptionKey;
    #endregion

    #region === Guide Content ===

    [Header("Guide Content")]
    public GuideImageEntry[] guideEntries;   // List of guide images with descriptions

    #endregion
}

#region === Data Models ===

/// <summary>
/// Single guide entry containing image + description.
/// </summary>
[System.Serializable]
public class GuideImageEntry
{
    [Header("Visual")]
    public Sprite image;                     // Guide illustration image

    [Header("Text")]
    [TextArea(2, 4)]
    public string description;               // Description for this image

    public string entryKey;
}

#endregion
