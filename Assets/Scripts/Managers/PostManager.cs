using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostManager : SingletonMonobehaviour<PostManager>
{
    #region === Inspector Fields ===

    [Header("References")]
    [SerializeField] private ReactionAndFollowerFX reactionFX;
    [SerializeField] private PostContainer postPrefab;
    [SerializeField] private PostDataSO[] postDataArray;
    [SerializeField] private Transform postParent;
    [SerializeField] private Button postButton;

    private PlayerControl playerControl;

    [Header("Settings")]
    [SerializeField] private Vector2 cooldownRange = new Vector2(15f, 25f);
    [SerializeField] private float decayRate = 0.5f;
    [SerializeField] private float rewardOnPost = 10f;
    [SerializeField] private float fxUpdateInterval = 5f;
    [SerializeField] private double moneyPerPost = 50.0;
    [SerializeField] private double moneyPerInterval = 5.0;
    [SerializeField] private float moneyInterval = 10f;

    [Header("UI Reactions")]
    [SerializeField] private TextMeshProUGUI likeText;
    [SerializeField] private TextMeshProUGUI heartText;
    [SerializeField] private TextMeshProUGUI angryText;

    [HideInInspector] public bool hasPostedFirstTime;
    [SerializeField] private bool canPost;

    #endregion

    #region === Internal State ===

    private Coroutine cooldownRoutine;
    private Coroutine decayRoutine;
    private Coroutine moneyRoutine;
    private readonly List<PostContainer> posts = new();

    private int likeCount, heartCount, angryCount;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        playerControl = PlayerControl.Instance;

        postButton.interactable = false;
        postButton.onClick.AddListener(OnPostButtonClicked);

        moneyRoutine = StartCoroutine(AutoAddMoney());
        playerControl.stats.OnStatsInitialized += OnStatsReady;
    }

    private void OnDestroy()
    {
        postButton.onClick.RemoveListener(OnPostButtonClicked);
        playerControl.stats.OnStatsInitialized -= OnStatsReady;
    }

    #endregion

    #region === Initialization ===

    // Called when PlayerStats finishes initializing
    private void OnStatsReady()
    {
        UpdateFXAndPosts();
        BeginCooldown();
    }

    #endregion

    #region === Post Button Logic ===

    // When user presses the post button
    private void OnPostButtonClicked()
    {
        if (!canPost) return;

        CreatePost();
        playerControl.stats.ApplyStatChange(StatType.IncomeRate, rewardOnPost);

        if (!hasPostedFirstTime)
            hasPostedFirstTime = true;

        UpdateFXAndPosts();
        BeginCooldown();
    }

    // Create a new post from random data and engagement level
    private void CreatePost()
    {
        var data = postDataArray[Random.Range(0, postDataArray.Length)];
        var post = Instantiate(postPrefab, postParent);
        var level = GetEngagementLevel(playerControl.stats.engagement.current);

        post.Configure(data.caption, data.image, level);
        posts.Add(post);
    }

    #endregion

    #region === Cooldown & Engagement Decay ===

    // Begin post cooldown and engagement decay (after first post)
    private void BeginCooldown()
    {
        canPost = false;
        postButton.interactable = false;

        if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(CooldownTimer());

        if (decayRoutine == null && hasPostedFirstTime)
            decayRoutine = StartCoroutine(DecayEngagement());
    }

    // Cooldown duration before next post is allowed
    private IEnumerator CooldownTimer()
    {
        yield return new WaitForSeconds(Random.Range(cooldownRange.x, cooldownRange.y));
        canPost = true;
        postButton.interactable = true;
    }

    // Continuously reduce engagement over time and update FX
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

    #region === Auto Money Gain ===

    // Passive money gain based on engagement level after first post
    private IEnumerator AutoAddMoney()
    {
        while (!hasPostedFirstTime)
            yield return null;

        while (true)
        {
            yield return new WaitForSeconds(moneyInterval);

            float currentValue = playerControl.stats.engagement.current;
            var level = GetEngagementLevel(currentValue);
            int multiplier = StatUpgradeManager.Instance.GetLevelOf(StatType.IncomeRate);
            double bonus = GetMoneyBonusByEngagementLevel(level);
            double totalMoney = moneyPerInterval * bonus * multiplier;

            MoneyManager.Instance.ChangeMoneys(totalMoney);
            AudioManager.Instance.PlayInteractSound(0);
        }
    }

    #endregion

    #region === FX & Post Updating ===

    // Update flying reactions and refresh post UI
    private void UpdateFXAndPosts()
    {
        float currentValue = playerControl.stats.engagement.GetPercentage();
        var level = GetEngagementLevel(currentValue);

        UpdateReactions(level);
        reactionFX.SetSpawnDistribution(level);

        foreach (var post in posts)
            post.SetEngagementValue(level);
    }

    // Add reaction counts based on engagement level
    private void UpdateReactions(EngagementLevel level)
    {
        int deltaMin = 1, deltaMax = 5;

        switch (level)
        {
            case EngagementLevel.Low:
                likeCount -= Random.Range(deltaMin, deltaMax + 1);
                heartCount -= Random.Range(deltaMin, deltaMax + 1);
                angryCount += Random.Range(deltaMax + 1, deltaMax + 4);
                break;

            case EngagementLevel.Medium:
                likeCount += Random.Range(deltaMin, deltaMax + 2);
                heartCount += Random.Range(deltaMin, deltaMax + 2);
                angryCount += Random.Range(deltaMin, deltaMax + 1);
                break;

            case EngagementLevel.High:
                likeCount += Random.Range(deltaMax, deltaMax + 3);
                heartCount += Random.Range(deltaMax, deltaMax + 3);
                angryCount -= Random.Range(deltaMin, deltaMax);
                break;

            case EngagementLevel.VeryHigh:
                likeCount += Random.Range(deltaMax + 2, deltaMax + 5);
                heartCount += Random.Range(deltaMax + 2, deltaMax + 5);
                angryCount -= Random.Range(deltaMax, deltaMax + 4);
                break;
        }

        int max = 10000;
        likeCount = Mathf.Clamp(likeCount, 0, max);
        heartCount = Mathf.Clamp(heartCount, 0, max);
        angryCount = Mathf.Clamp(angryCount, 0, max);

        UpdateReactionUI();
    }

    // Refresh reaction UI texts
    private void UpdateReactionUI()
    {
        heartText.text = heartCount.ToString();
        likeText.text = likeCount.ToString();
        angryText.text = angryCount.ToString();
    }

    #endregion

    #region === Helpers ===

    // Convert engagement float to tier
    private EngagementLevel GetEngagementLevel(float value)
    {
        if (value <= .3f) return EngagementLevel.Low;
        if (value <= .6f) return EngagementLevel.Medium;
        if (value <= .9f) return EngagementLevel.High;
        return EngagementLevel.VeryHigh;
    }

    // Money bonus multiplier per engagement level
    private double GetMoneyBonusByEngagementLevel(EngagementLevel level) => level switch
    {
        EngagementLevel.Low => 0.5,
        EngagementLevel.Medium => 1.0,
        EngagementLevel.High => 1.5,
        EngagementLevel.VeryHigh => 2.0,
        _ => 0.0
    };

    #endregion
}
