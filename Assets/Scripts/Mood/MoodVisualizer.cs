using UnityEngine;
using UnityEngine.UI;

public class MoodVisualizer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image moodIconImage;

    [Header("Face Materials")]
    [SerializeField] private Material eyeMat;
    [SerializeField] private Material mouthMat;

    [Header("Face Sprite Sets")]
    [SerializeField] private FaceMaterialSetSO[] faceMaterialSets;

    private MoodConditionDataSO currentMood;

    public void SetMoodVisual(MoodConditionDataSO mood)
    {
        currentMood = mood;

        if (mood == null)
        {
            ClearMoodVisual();
            return;
        }

        // Hiển thị icon UI
        if (moodIconImage != null && mood.moodIcon != null)
        {
            moodIconImage.sprite = mood.moodIcon;
            moodIconImage.enabled = true;
        }

        // Gán texture từ sprite → material
        FaceMaterialSetSO matchedSet = FindMaterialSet(mood.conditionType);

        if (matchedSet != null)
        {
            if (eyeMat != null && matchedSet.eyeSprite != null)
                eyeMat.mainTexture = matchedSet.eyeSprite.texture;

            if (mouthMat != null && matchedSet.mouthSprite != null)
                mouthMat.mainTexture = matchedSet.mouthSprite.texture;
        }
    }

    public void ClearMoodVisual()
    {
        currentMood = null;

        if (moodIconImage != null)
        {
            moodIconImage.sprite = null;
            moodIconImage.enabled = false;
        }

        // Gán lại texture mặc định theo trạng thái "Normal"
        FaceMaterialSetSO defaultSet = FindMaterialSet(MoodConditionType.Normal);

        if (defaultSet != null)
        {
            if (eyeMat != null && defaultSet.eyeSprite != null)
                eyeMat.mainTexture = defaultSet.eyeSprite.texture;

            if (mouthMat != null && defaultSet.mouthSprite != null)
                mouthMat.mainTexture = defaultSet.mouthSprite.texture;
        }
    }

    private FaceMaterialSetSO FindMaterialSet(MoodConditionType type)
    {
        foreach (var set in faceMaterialSets)
        {
            if (set != null && set.conditionType == type)
                return set;
        }

        return null;
    }
}
