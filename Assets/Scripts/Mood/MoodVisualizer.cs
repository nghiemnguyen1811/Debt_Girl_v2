using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

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

    [Header("Mood Animation Loop")]
    [SerializeField] private float minAnimDelay = 10f;
    [SerializeField] private float maxAnimDelay = 20f;

    private PlayerControl playerControl;
    private MoodConditionDataSO currentMood;
    private Tween moodTween;
    private Coroutine moodAnimRoutine;
    private Vector3 originalIconLocalPosition;

    void Start()
    {
        playerControl = GetComponent<PlayerControl>();
        originalIconLocalPosition = moodIconRoot.transform.localPosition;
        moodIconRoot.SetActive(false);
    }

    public void SetMoodVisual(MoodConditionDataSO mood)
    {
        currentMood = mood;

        if (mood == null)
        {
            ClearMoodVisual();
            return;
        }

        ApplyMoodIcon(mood);
        ApplyFaceTextures(mood.conditionType);
        StartMoodAnimationLoop();
    }

    public void ClearMoodVisual()
    {
        currentMood = null;
        moodTween?.Kill();

        moodIconImage.DOFade(0f, 0.3f).OnComplete(() =>
        {
            moodIconRoot.SetActive(false);
            moodIconImage.sprite = null;
        });

        ApplyFaceTextures(MoodConditionType.Normal);
        StopMoodAnimationLoop();
        ResetMoodIconPosition();
    }

    private void ApplyMoodIcon(MoodConditionDataSO mood)
    {
        if (moodIconImage == null || mood.moodIcon == null) return;

        moodIconImage.sprite = mood.moodIcon;
        moodIconRoot.SetActive(true);
        moodIconRoot.transform.localScale = Vector3.zero;
        moodIconImage.color = new Color(1, 1, 1, 0);

        moodTween?.Kill();
        moodTween = moodIconRoot.transform
            .DOScale(popupScale, popupDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                moodIconRoot.transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
            });

        moodIconImage.DOFade(1f, 0.3f).SetEase(Ease.InOutSine);
    }

    private void ApplyFaceTextures(MoodConditionType type)
    {
        var set = FindMaterialSet(type);

        if (set == null) return;

        if (eyeMat != null && set.eyeSprite != null)
            eyeMat.mainTexture = set.eyeSprite.texture;

        if (mouthMat != null && set.mouthSprite != null)
            mouthMat.mainTexture = set.mouthSprite.texture;
    }

    private void StartMoodAnimationLoop()
    {
        if (moodAnimRoutine != null)
            StopCoroutine(moodAnimRoutine);

        moodAnimRoutine = StartCoroutine(MoodAnimationLoop());
    }

    private void StopMoodAnimationLoop()
    {
        if (moodAnimRoutine != null)
        {
            StopCoroutine(moodAnimRoutine);
            moodAnimRoutine = null;
        }
    }

    private IEnumerator MoodAnimationLoop()
    {
        while (currentMood != null)
        {
            yield return new WaitForSeconds(Random.Range(minAnimDelay, maxAnimDelay));

            if (playerControl == null || currentMood == null ||
                playerControl.interactDetector?.IsInteracting == true)
                continue;

            playerControl.animationHandler.SetMoodTrigger(currentMood.moodAnimName, currentMood.animatorLayerIndex);
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

    public void OffsetMoodIcon(Vector3 newLocalPosition)
    {
        if (moodIconRoot != null)
            moodIconRoot.transform.localPosition = newLocalPosition;
    }

    public void ResetMoodIconPosition()
    {
        if (moodIconRoot != null)
            moodIconRoot.transform.localPosition = originalIconLocalPosition;
    }
}
