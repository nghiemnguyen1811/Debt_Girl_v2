using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using URandom = UnityEngine.Random;

/// <summary>
/// Manages posting: create posts, cooldown, engagement decay,
/// auto-income, FX updates, and UI updates.
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
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private Button postButton;

    private PlayerControl playerControl;

    [Header("Posting Settings")]
    [SerializeField] private Vector2 cooldownRange = new Vector2(20f, 40f);
    [SerializeField] private float minMoodToPost = 30f;
    [SerializeField] private bool canPost;
    [HideInInspector] public bool hasPostedFirstTime;

    [Header("Engagement Settings")]
    [SerializeField] private float decayRate = 0.1f;
    [SerializeField] private float fxUpdateInterval = 5f;

    [Header("Money Settings")]
    [SerializeField] private double moneyPerInterval = 5.0;
    [SerializeField] private float moneyInterval = 10f;
    [SerializeField] private float rewardOnPost = 10f;

    [Header("Animation Settings")]
    [SerializeField] private float floatingTextFadeDuration = 2f;

    [Header("Mood Warning Messages")]
    [TextArea(2, 5)]
    [SerializeField]
    private string[] warningMessages = {
        "기분이 부족합니다.",
        "너무 기분이 다운돼서 할 수 없습니다.",
        "먼저 기분을 회복하는 게 좋습니다.",
        "기분이 너무 낮습니다."
    };

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Internal State ===

    private Coroutine cooldownRoutine;
    private Coroutine decayRoutine;
    private Coroutine moneyRoutine;

    private readonly List<PostContainer> posts = new();
    private Sequence warningSequence;

    private int postCount = 0;

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Unity Events ===

    private void OnEnable()
    {
        // Setup references
        playerControl = PlayerControl.Instance;

        // Disable post button at start
        postButton.interactable = false;

        // Start passive money routine
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
    #region === Initialization ===

    /// <summary>Called when PlayerStats are fully initialized.</summary>
    private void OnStatsReady()
    {
        UpdateFXAndPosts();
        BeginCooldown();
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Post Button Logic ===

    /// <summary>Handles Post Button click.</summary>
    private void OnPostButtonClicked()
    {
        if (!canPost || playerControl.stats.mood.current < 30)
        {
            ShowWarningText(warningMessages[URandom.Range(0, warningMessages.Length)]);
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

    /// <summary>Creates a new post using random data.</summary>
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
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Cooldown & Engagement Decay ===

    /// <summary>Begins cooldown and starts decay if first post was created.</summary>
    private void BeginCooldown()
    {
        canPost = false;

        if (postButton == null || postButton.gameObject == null)
        {
            Debug.LogWarning("[PostManager] postButton missing → skip cooldown");
            return;
        }

        postButton.interactable = false;

        if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(CooldownTimer());

        if (decayRoutine == null && hasPostedFirstTime)
            decayRoutine = StartCoroutine(DecayEngagement());
    }

    /// <summary>Cooldown timer before next post allowed.</summary>
    private IEnumerator CooldownTimer()
    {
        yield return new WaitForSeconds(URandom.Range(cooldownRange.x, cooldownRange.y));
        canPost = true;
        postButton.interactable = true;
    }

    /// <summary>Gradually decreases engagement and updates FX periodically.</summary>
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

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Auto Money Gain ===

    /// <summary>Adds passive income every interval after first post.</summary>
    private IEnumerator AutoAddMoney()
    {
        while (!hasPostedFirstTime)
            yield return null;

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
    #region === FX & Post Updating ===

    /// <summary>Updates reaction FX and all posts’ engagement UI.</summary>
    private void UpdateFXAndPosts()
    {
        float pct = playerControl.stats.engagement.GetPercentage();
        var level = GetEngagementLevel(pct);

        reactionFX.SetSpawnDistribution(level);

        foreach (var post in posts)
            post.SetEngagementValue(level);

        UpdatePostLevelUI(level);
    }

    /// <summary>Updates UI for total post count.</summary>
    private void UpdatePostCountUI()
    {
        if (postCountText != null)
            postCountText.text = postCount.ToString();
    }

    /// <summary>Updates UI for engagement level.</summary>
    private void UpdatePostLevelUI(EngagementLevel level)
    {
        if (postLevelText != null)
            postLevelText.text = "Lv." + ConvertLevelToInt(level);
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Warning System ===

    /// <summary>Shows animated mood warning text.</summary>
    private void ShowWarningText(string message)
    {
        if (warningSequence != null && warningSequence.IsActive())
            warningSequence.Kill();

        warningText.gameObject.SetActive(true);
        warningText.text = message;
        warningText.transform.localScale = Vector3.one;
        warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1f);

        warningSequence = DOTween.Sequence()
            .Append(warningText.transform.DOScale(Vector3.one * 1.2f, 0.1f).SetEase(Ease.OutBack))
            .Append(warningText.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack))
            .AppendInterval(0.5f)
            .Append(warningText.DOFade(0f, floatingTextFadeDuration).SetEase(Ease.InOutQuad))
            .OnComplete(() =>
            {
                warningText.gameObject.SetActive(false);
                warningText.text = "";
                warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, 1f);
            });
    }

    #endregion

    //─────────────────────────────────────────────────────────────
    #region === Helpers ===

    /// <summary>Converts percentage to engagement tier.</summary>
    private EngagementLevel GetEngagementLevel(float value)
    {
        if (value <= .3f) return EngagementLevel.Low;
        if (value <= .6f) return EngagementLevel.Medium;
        if (value <= .9f) return EngagementLevel.High;
        return EngagementLevel.VeryHigh;
    }

    /// <summary>Returns money multiplier based on engagement tier.</summary>
    private double GetMoneyBonusByEngagementLevel(EngagementLevel level) => level switch
    {
        EngagementLevel.Low => 0.5,
        EngagementLevel.Medium => 1.0,
        EngagementLevel.High => 1.5,
        EngagementLevel.VeryHigh => 2.0,
        _ => 0
    };

    /// <summary>Converts EngagementLevel to int.</summary>
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

    /// <summary>Saves hasPostedFirstTime flag.</summary>
    public void AutoSave()
    {
        if (SaveManager.Data == null) return;

        SaveManager.Data.hasPostedFirstTime = hasPostedFirstTime;
        SaveManager.SaveGame();
    }

    /// <summary>Applies loaded save data.</summary>
    public void ImportSaveData(SaveData data)
    {
        if (data == null) return;

        hasPostedFirstTime = data.hasPostedFirstTime;

        if (hasPostedFirstTime)
            StartCoroutine(WaitForUIReadyThenCooldown());
    }


    private IEnumerator WaitForUIReadyThenCooldown()
    {
        yield return new WaitUntil(() => postButton != null && postButton.gameObject != null);

        BeginCooldown();

        if (moneyRoutine == null)
            moneyRoutine = StartCoroutine(AutoAddMoney());
    }

    #endregion
}
