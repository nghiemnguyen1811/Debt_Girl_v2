using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MoodVisualizer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject moodIconRoot;
    [SerializeField] private Image moodIconImage;

    [Header("Face Materials")]
    [SerializeField] private Material eyeMat;
    [SerializeField] private Material mouthMat;

    [Header("Face Sprite Sets")]
    [SerializeField] private FaceMaterialSetSO[] faceMaterialSets;

    [Header("Popup Effect")]
    [SerializeField] private float popupScale = 1.3f;
    [SerializeField] private float popupDuration = 0.4f;

    private MoodConditionDataSO currentMood;
    private Tween moodTween;

    // === MỚI: Lưu vị trí gốc của moodIconRoot ===
    private Vector3 originalIconLocalPosition;

    private void Start()
    {
        if (moodIconRoot != null)
        {
            originalIconLocalPosition = moodIconRoot.transform.localPosition;
            moodIconRoot.SetActive(false); // Ẩn khi start
        }
    }

    public void SetMoodVisual(MoodConditionDataSO mood)
    {
        currentMood = mood;

        if (mood == null)
        {
            ClearMoodVisual();
            return;
        }

        if (moodIconRoot != null)
            moodIconRoot.SetActive(true);

        if (moodIconImage != null && mood.moodIcon != null)
        {
            moodIconImage.sprite = mood.moodIcon;
            moodIconImage.enabled = true;

            moodIconImage.transform.localScale = Vector3.zero;
            moodIconImage.color = new Color(1, 1, 1, 0);

            moodTween?.Kill();
            moodTween = moodIconImage.transform
                .DOScale(popupScale, popupDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    moodIconImage.transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
                });

            moodIconImage.DOFade(1f, 0.3f).SetEase(Ease.InOutSine);
        }

        var matchedSet = FindMaterialSet(mood.conditionType);
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
        moodTween?.Kill();

        if (moodIconImage != null)
        {
            moodIconImage.DOFade(0f, 0.3f).OnComplete(() =>
            {
                moodIconImage.enabled = false;
                moodIconImage.sprite = null;
            });
        }

        if (moodIconRoot != null)
            moodIconRoot.SetActive(false);

        var defaultSet = FindMaterialSet(MoodConditionType.Normal);
        if (defaultSet != null)
        {
            if (eyeMat != null && defaultSet.eyeSprite != null)
                eyeMat.mainTexture = defaultSet.eyeSprite.texture;

            if (mouthMat != null && defaultSet.mouthSprite != null)
                mouthMat.mainTexture = defaultSet.mouthSprite.texture;
        }

        ResetMoodIconPosition();
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

    public void OffsetMoodIcon(Vector3 offset)
    {
        if (moodIconRoot == null) return;

        moodIconRoot.transform.localPosition = offset;
    }

    public void ResetMoodIconPosition()
    {
        if (moodIconRoot == null) return;

        moodIconRoot.transform.localPosition = originalIconLocalPosition;
    }
}
