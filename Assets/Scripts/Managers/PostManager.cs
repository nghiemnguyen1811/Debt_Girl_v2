using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using URandom = UnityEngine.Random;

/// <summary>
/// Manages posting logic. Warning UI logic has been moved to GameManager.
/// </summary>
public class PostManager : SingletonMonobehaviour<PostManager>
{
    public event Action OnPostCreated;

    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("FX & Prefab References")]
    [SerializeField] private ReactionAndFollowerFX reactionFX;
    [SerializeField] private PostContainer postPrefab;
    [SerializeField] private PostDataSO[] postDataArray;
    [SerializeField] private RectTransform postParent;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI postCountText;
    [SerializeField] private TextMeshProUGUI postLevelText;
    [SerializeField] private Button postButton;
    [SerializeField] private Image postEnableFillImage;

    private PlayerControl playerControl;

    [Header("Posting Settings")]
    [SerializeField] private Vector2 cooldownRange = new Vector2(20f, 40f);
    [SerializeField] private float minMoodToPost = 30f; // Fixed: Now used in logic
    [SerializeField] private bool canPost;
    [HideInInspector] public bool hasPostedFirstTime;

    [Header("Engagement Settings")]
    [SerializeField] private float decayRate = 0.1f;
    [SerializeField] private float fxUpdateInterval = 5f;

    [Header("Money Settings")]
    [SerializeField] private double moneyPerInterval = 5.0;
    [SerializeField] private float moneyInterval = 10f;
    [SerializeField] private float rewardOnPost = 10f;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Internal State ===

    private Coroutine cooldownRoutine;
    private Coroutine decayRoutine;
    private Coroutine moneyRoutine;

    private readonly List<PostContainer> posts = new();
    private int postCount = 0;
    private Tween postCooldownFillTween;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Events ===

    private void OnEnable()
    {
        playerControl = PlayerControl.Instance;
        postButton.interactable = false;

        if (postEnableFillImage != null) postEnableFillImage.fillAmount = 0f;

        moneyRoutine = StartCoroutine(AutoAddMoney());

        postButton.onClick.AddListener(OnPostButtonClicked);
        playerControl.stats.OnStatsInitialized += OnStatsReady;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        postButton.onClick.RemoveListener(OnPostButtonClicked);
        playerControl.stats.OnStatsInitialized -= OnStatsReady;
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Core Logic ===

    private void OnStatsReady()
    {
        UpdateFXAndPosts();
        BeginCooldown();
    }

    /// <summary>Refactored Post Button Logic: Uses GameManager for Warnings.</summary>
    private void OnPostButtonClicked()
    {
        // Check mood using the Inspector variable 'minMoodToPost'
        if (!canPost || playerControl.stats.mood.current < minMoodToPost)
        {
            // Call GameManager to handle the UI Warning
            GameManager.Instance.ShowMoodWarning();
            return;
        }

        CreatePost();
        playerControl.stats.ApplyStatChange(StatType.IncomeRate, rewardOnPost);

        if (!hasPostedFirstTime)
        {
            hasPostedFirstTime = true;
            AutoSave();
        }

        UpdateFXAndPosts();
        BeginCooldown();

        AudioManager.Instance.PlayInteractSound(8);
    }

    private void CreatePost()
    {
        var data = postDataArray[URandom.Range(0, postDataArray.Length)];
        var post = Instantiate(postPrefab, postParent);

        var level = GetEngagementLevel(playerControl.stats.engagement.current);
        post.Configure(data.caption, data.image, level);
        posts.Add(post);

        postCount++;
        UpdatePostCountUI();

        LayoutRebuilder.ForceRebuildLayoutImmediate(postParent);
        OnPostCreated?.Invoke();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Cooldown & Engagement ===

    private void BeginCooldown()
    {
        canPost = false;
        if (postButton == null) return;

        postButton.interactable = false;
        postCooldownFillTween?.Kill();

        if (postEnableFillImage != null)
        {
            postEnableFillImage.type = Image.Type.Filled;
            postEnableFillImage.fillAmount = 0f;
        }

        if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(CooldownTimer());

        if (decayRoutine == null && hasPostedFirstTime)
            decayRoutine = StartCoroutine(DecayEngagement());
    }

    private IEnumerator CooldownTimer()
    {
        float duration = URandom.Range(cooldownRange.x, cooldownRange.y);

        if (postEnableFillImage != null)
        {
            postCooldownFillTween = postEnableFillImage
                .DOFillAmount(1f, duration)
                .SetEase(Ease.Linear);
        }

        yield return new WaitForSeconds(duration);

        canPost = true;
        postButton.interactable = true;

        if (postEnableFillImage != null)
            postEnableFillImage.fillAmount = 1f;
    }

    private IEnumerator DecayEngagement()
    {
        float timer = 0f;
        while (true)
        {
            playerControl.stats.ApplyStatChange(StatType.IncomeRate, -decayRate * Time.deltaTime);
            timer += Time.deltaTime;

            if (timer >= fxUpdateInterval)
            {
                timer = 0f;
                UpdateFXAndPosts();
            }
            yield return null;
        }
    }

    private IEnumerator AutoAddMoney()
    {
        while (!hasPostedFirstTime) yield return null;

        while (true)
        {
            yield return new WaitForSeconds(moneyInterval);

            float value = playerControl.stats.engagement.current;
            var level = GetEngagementLevel(value);

            int multiplier = StatUpgradeManager.Instance.GetLevelOf(StatType.IncomeRate);
            double bonus = GetMoneyBonusByEngagementLevel(level);
            double total = moneyPerInterval * bonus * multiplier;

            MoneyManager.Instance.ChangeMoneys(total);
            AudioManager.Instance.PlayInteractSound(0);
        }
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Helpers & UI ===

    private void UpdateFXAndPosts()
    {
        float pct = playerControl.stats.engagement.GetPercentage();
        var level = GetEngagementLevel(pct);

        reactionFX.SetSpawnDistribution(level);
        foreach (var post in posts) post.SetEngagementValue(level);
        UpdatePostLevelUI(level);
    }

    private void UpdatePostCountUI()
    {
        if (postCountText != null) postCountText.text = postCount.ToString();
    }

    private void UpdatePostLevelUI(EngagementLevel level)
    {
        if (postLevelText != null) postLevelText.text = "Lv." + ConvertLevelToInt(level);
    }

    private EngagementLevel GetEngagementLevel(float value)
    {
        if (value <= .3f) return EngagementLevel.Low;
        if (value <= .6f) return EngagementLevel.Medium;
        if (value <= .9f) return EngagementLevel.High;
        return EngagementLevel.VeryHigh;
    }

    private double GetMoneyBonusByEngagementLevel(EngagementLevel level) => level switch
    {
        EngagementLevel.Low => 0.5,
        EngagementLevel.Medium => 1.0,
        EngagementLevel.High => 1.5,
        EngagementLevel.VeryHigh => 2.0,
        _ => 0
    };

    private int ConvertLevelToInt(EngagementLevel level) => level switch
    {
        EngagementLevel.Low => 1,
        EngagementLevel.Medium => 2,
        EngagementLevel.High => 3,
        EngagementLevel.VeryHigh => 4,
        _ => 0
    };

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Save / Load ===

    public void AutoSave()
    {
        if (SaveManager.Data == null) return;
        SaveManager.Data.hasPostedFirstTime = hasPostedFirstTime;
        SaveManager.SaveGame();
    }

    public void ImportSaveData(SaveData data)
    {
        if (data == null) return;
        hasPostedFirstTime = data.hasPostedFirstTime;

        if (hasPostedFirstTime)
            StartCoroutine(WaitForUIReadyThenCooldown());
    }

    private IEnumerator WaitForUIReadyThenCooldown()
    {
        yield return new WaitUntil(() => postButton != null);
        BeginCooldown();
        if (moneyRoutine == null) moneyRoutine = StartCoroutine(AutoAddMoney());
    }

    #endregion
}