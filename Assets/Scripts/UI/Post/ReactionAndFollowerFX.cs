using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ReactionAndFollowerFX : MonoBehaviour
{
    #region === Inspector Fields ===

    [Header("Parent Containers")]
    [SerializeField] private RectTransform heartParent;
    [SerializeField] private RectTransform likeParent;
    [SerializeField] private RectTransform angryParent;

    [Header("Follower Settings")]
    [SerializeField] private int followerCount = 1000;
    [SerializeField] private TextMeshProUGUI followerText;
    [SerializeField] private float followerChangeDuration = 1.5f;

    private Tween followerTween;

    [Header("Canvas Area")]
    [SerializeField] private RectTransform spawnAreaCanvas;

    [Header("Fly Settings")]
    [SerializeField] private float minFlyDuration = 2.5f;
    [SerializeField] private float maxFlyDuration = 3.5f;
    [SerializeField] private float minInterval = 0.6f;
    [SerializeField] private float maxInterval = 1.2f;
    [SerializeField] private int minSpawnPerWave = 2;
    [SerializeField] private int maxSpawnPerWave = 5;
    [SerializeField] private Vector2 startScaleRange = new Vector2(1.0f, 1.8f);

    [Header("Noise Settings")]
    [SerializeField] private float noiseAmplitude = 30f;
    [SerializeField] private float noiseFrequency = 3f;

    #endregion

    #region === Internal Data Structures ===

    private enum ReactionType { Heart, Like, Angry }

    private class FlyingReaction
    {
        public RectTransform rect;
        public ReactionType type;
        public CanvasGroup canvasGroup;
    }

    private readonly List<FlyingReaction> allObjects = new();
    private Dictionary<ReactionType, float> reactionWeights = new();
    private EngagementLevel engagementValue = EngagementLevel.Medium;
    private EngagementLevel? pendingValue = null;
    private Coroutine spawnRoutine;
    private Coroutine followerRoutine;

    #endregion

    #region === Unity Events ===

    // Initialize reaction object pools on startup
    private void Start()
    {
        InitFromParent(heartParent, ReactionType.Heart);
        InitFromParent(likeParent, ReactionType.Like);
        InitFromParent(angryParent, ReactionType.Angry);
    }

    // Apply pending engagement value if component is re-enabled
    private void OnEnable()
    {
        if (!PostManager.Instance.hasPostedFirstTime) return;

        if (pendingValue.HasValue)
        {
            engagementValue = pendingValue.Value;
            ApplyDistribution(engagementValue);
            pendingValue = null;
        }
        else ApplyDistribution(engagementValue);
    }

    #endregion

    #region === Public API ===

    // Allows external systems to update engagement level for reaction behavior
    public void SetSpawnDistribution(EngagementLevel engagementLevel)
    {
        if (!PostManager.Instance.hasPostedFirstTime) return;

        if (!gameObject.activeInHierarchy)
            pendingValue = engagementLevel;
        else
            ApplyDistribution(engagementLevel);
    }

    #endregion

    #region === Distribution and Spawning Logic ===

    // Set reaction spawn weights based on engagement level
    private void ApplyDistribution(EngagementLevel engagementLevel)
    {
        engagementValue = engagementLevel;
        reactionWeights.Clear();

        switch (engagementLevel)
        {
            case EngagementLevel.Low:
                reactionWeights[ReactionType.Angry] = 0.6f;
                reactionWeights[ReactionType.Heart] = 0.2f;
                reactionWeights[ReactionType.Like] = 0.2f;
                break;
            case EngagementLevel.Medium:
                reactionWeights[ReactionType.Angry] = 0.33f;
                reactionWeights[ReactionType.Heart] = 0.33f;
                reactionWeights[ReactionType.Like] = 0.34f;
                break;
            case EngagementLevel.High:
                reactionWeights[ReactionType.Angry] = 0.1f;
                reactionWeights[ReactionType.Heart] = 0.45f;
                reactionWeights[ReactionType.Like] = 0.45f;
                break;
            case EngagementLevel.VeryHigh:
                reactionWeights[ReactionType.Heart] = 0.5f;
                reactionWeights[ReactionType.Like] = 0.5f;
                break;
        }

        RestartCoroutines();
    }

    // Stop existing coroutines and restart them with new settings
    private void RestartCoroutines()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        if (followerRoutine != null) StopCoroutine(followerRoutine);

        spawnRoutine = StartCoroutine(LoopSpawnWave());
        followerRoutine = StartCoroutine(LoopFollowerChange());
    }

    // Periodically spawn reactions based on weighted distribution
    private IEnumerator LoopSpawnWave()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            int spawnCount = Random.Range(minSpawnPerWave, maxSpawnPerWave + 1);

            for (int i = 0; i < spawnCount; i++)
            {
                var type = GetWeightedReactionType();
                var reaction = GetInactiveOfType(type);

                if (reaction != null)
                    AnimateFlyBezierWithNoise(reaction);
            }
        }
    }

    // Select a random reaction type based on current weight distribution
    private ReactionType GetWeightedReactionType()
    {
        float total = 0f;
        foreach (var w in reactionWeights.Values)
            total += w;

        float rand = Random.Range(0f, total);
        float accum = 0f;

        foreach (var kvp in reactionWeights)
        {
            accum += kvp.Value;
            if (rand <= accum)
                return kvp.Key;
        }

        return ReactionType.Heart;
    }

    #endregion

    #region === Follower Count Animation ===

    // Loop to continuously adjust follower count based on engagement level
    private IEnumerator LoopFollowerChange()
    {
        while (true)
        {
            int delta = 0;

            switch (engagementValue)
            {
                case EngagementLevel.Low:
                    delta = -Random.Range(2, 5);
                    break;
                case EngagementLevel.Medium:
                    delta = 0;
                    break;
                case EngagementLevel.High:
                    delta = Random.Range(1, 4);
                    break;
                case EngagementLevel.VeryHigh:
                    Random.Range(5, 10);
                    break;
            }

            int newCount = Mathf.Max(0, followerCount + delta);
            AnimateFollowerChange(newCount);

            yield return new WaitForSeconds(2f);
        }
    }

    // Smoothly animate the follower count UI with DOTween
    private void AnimateFollowerChange(int newCount)
    {
        if (followerText == null) return;

        if (followerTween != null && followerTween.IsActive())
            followerTween.Kill();

        int currentDisplay = followerCount;
        followerTween = DOTween.To(() => currentDisplay, x =>
        {
            currentDisplay = x;
            followerText.text = currentDisplay.ToString();
        },
        newCount,
        followerChangeDuration).SetEase(Ease.OutCubic);

        followerText.text = DoubleUtilities.ToIdleNotation(currentDisplay);
        followerCount = newCount;
    }

    #endregion

    #region === Reaction Fly Animation ===

    // Animate flying icon using Bezier curve with sinusoidal noise
    private void AnimateFlyBezierWithNoise(FlyingReaction reaction)
    {
        RectTransform rect = reaction.rect;
        CanvasGroup canvasGroup = reaction.canvasGroup;

        rect.gameObject.SetActive(true);
        canvasGroup.alpha = 0.8f;

        float startScale = Random.Range(startScaleRange.x, startScaleRange.y);
        rect.localScale = Vector3.one * startScale;

        Vector2 canvasSize = spawnAreaCanvas.rect.size;

        Vector2 p0 = new Vector2(
            canvasSize.x * 0.5f + Random.Range(100f, 200f),
            -canvasSize.y * 0.5f - Random.Range(100f, 150f)
        );
        Vector2 p1 = new Vector2(
            -canvasSize.x * 0.25f + Random.Range(-50f, 50f),
            -canvasSize.y * 0.25f + Random.Range(-50f, 50f)
        );
        Vector2 p2 = new Vector2(
            Random.Range(-50f, 50f),
            canvasSize.y * 0.5f + Random.Range(160f, 220f)
        );

        float duration = Random.Range(minFlyDuration, maxFlyDuration);
        float noisePhase = Random.Range(0f, 2f * Mathf.PI);
        float noiseAmp = Random.Range(noiseAmplitude * 0.5f, noiseAmplitude);
        float noiseFreq = Random.Range(noiseFrequency * 0.8f, noiseFrequency * 1.2f);

        DOTween.To(() => 0f, t =>
        {
            Vector2 pos = Mathf.Pow(1 - t, 2) * p0 +
                          2 * (1 - t) * t * p1 +
                          Mathf.Pow(t, 2) * p2;

            pos.x += Mathf.Sin(Time.time * noiseFreq + noisePhase) * noiseAmp * (1 - t);
            rect.anchoredPosition = pos;

            float fadeStart = 0.85f;
            float scale = t < fadeStart ? startScale : Mathf.Lerp(startScale, 0.6f, Mathf.InverseLerp(fadeStart, 1f, t));
            float alpha = t < fadeStart ? 0.8f : Mathf.Lerp(0.8f, 0f, Mathf.InverseLerp(fadeStart, 1f, t));

            rect.localScale = Vector3.one * scale;
            canvasGroup.alpha = alpha;

        }, 1f, duration).SetEase(Ease.Linear).OnComplete(() =>
        {
            rect.gameObject.SetActive(false);
        });
    }

    #endregion

    #region === Object Pool Initialization ===

    // Initialize object pool from parent container for a specific reaction type
    private void InitFromParent(RectTransform parent, ReactionType type)
    {
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child is RectTransform rect)
            {
                rect.gameObject.SetActive(false);

                CanvasGroup group = rect.GetComponent<CanvasGroup>();
                if (group == null)
                    group = rect.gameObject.AddComponent<CanvasGroup>();

                allObjects.Add(new FlyingReaction
                {
                    rect = rect,
                    type = type,
                    canvasGroup = group
                });
            }
        }
    }

    // Retrieve an inactive reaction object from the pool by type
    private FlyingReaction GetInactiveOfType(ReactionType type)
    {
        foreach (var obj in allObjects)
        {
            if (!obj.rect.gameObject.activeSelf && obj.type == type)
                return obj;
        }

        return null;
    }

    #endregion
}
