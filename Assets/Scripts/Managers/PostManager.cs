using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using URandom = UnityEngine.Random;

/// <summary>
/// Manages the posting system: creating posts, handling cooldown,
/// engagement decay, auto-money gain, and updating UI.
/// </summary>
public class PostManager : SingletonMonobehaviour<PostManager>
{
    public event Action OnPostCreated;

    #region === Inspector Fields ===

    [Header("References")]
    [SerializeField] private ReactionAndFollowerFX reactionFX;
    [SerializeField] private PostContainer postPrefab;
    [SerializeField] private PostDataSO[] postDataArray;
    [SerializeField] private RectTransform postParent;
    [SerializeField] private TextMeshProUGUI postCountText;
    [SerializeField] private TextMeshProUGUI postLevelText;
    [SerializeField] private Button postButton;

    private PlayerControl playerControl;

    [Header("Settings")]
    [SerializeField] private Vector2 cooldownRange = new Vector2(20f, 40f);
    [SerializeField] private float decayRate = 0.1f;
    [SerializeField] private float rewardOnPost = 10f;
    [SerializeField] private float fxUpdateInterval = 5f;
    [SerializeField] private double moneyPerInterval = 5.0;
    [SerializeField] private float moneyInterval = 10f;
    [HideInInspector] public bool hasPostedFirstTime;
    [SerializeField] private bool canPost;

    #endregion

    #region === Internal State ===

    private Coroutine cooldownRoutine;
    private Coroutine decayRoutine;
    private Coroutine moneyRoutine;
    private readonly List<PostContainer> posts = new();

    private int postCount = 0;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        playerControl = PlayerControl.Instance;

        // Disable post button at start
        postButton.interactable = false;
        postButton.onClick.AddListener(OnPostButtonClicked);

        // Start auto money routine
        moneyRoutine = StartCoroutine(AutoAddMoney());

        // Wait for PlayerStats to initialize
        playerControl.stats.OnStatsInitialized += OnStatsReady;
    }

    private void OnDestroy()
    {
        postButton.onClick.RemoveListener(OnPostButtonClicked);
        playerControl.stats.OnStatsInitialized -= OnStatsReady;
    }

    #endregion

    #region === Initialization ===

    /// <summary>
    /// Called once PlayerStats are ready.
    /// Initializes FX and starts cooldown.
    /// </summary>
    private void OnStatsReady()
    {
        UpdateFXAndPosts();
        BeginCooldown();
    }

    #endregion

    #region === Post Button Logic ===

    /// <summary>
    /// Triggered when the Post Button is clicked.
    /// </summary>
    private void OnPostButtonClicked()
    {
        if (!canPost) return;

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

    /// <summary>
    /// Creates a new post with random data and engagement level.
    /// </summary>
    private void CreatePost()
    {
        var data = postDataArray[URandom.Range(0, postDataArray.Length)];
        var post = Instantiate(postPrefab, postParent);
        var level = GetEngagementLevel(playerControl.stats.engagement.current);

        post.Configure(data.caption, data.image, level);
        posts.Add(post);

        // Increment post count and update UI
        postCount++;
        UpdatePostCountUI();

        LayoutRebuilder.ForceRebuildLayoutImmediate(postParent);
    }

    #endregion

    #region === Cooldown & Engagement Decay ===

    /// <summary>
    /// Starts cooldown after posting and begins engagement decay.
    /// </summary>
    private void BeginCooldown()
    {
        canPost = false;
        postButton.interactable = false;

        if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(CooldownTimer());

        if (decayRoutine == null && hasPostedFirstTime)
            decayRoutine = StartCoroutine(DecayEngagement());
    }

    /// <summary>
    /// Handles cooldown duration before next post is allowed.
    /// </summary>
    private IEnumerator CooldownTimer()
    {
        yield return new WaitForSeconds(URandom.Range(cooldownRange.x, cooldownRange.y));
        canPost = true;
        postButton.interactable = true;
    }

    /// <summary>
    /// Continuously reduces engagement over time.
    /// Updates FX every interval.
    /// </summary>
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

    /// <summary>
    /// Automatically adds passive money gain after first post.
    /// </summary>
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

    /// <summary>
    /// Updates flying reactions and refreshes all posts' engagement UI.
    /// </summary>
    private void UpdateFXAndPosts()
    {
        float currentValue = playerControl.stats.engagement.GetPercentage();
        var level = GetEngagementLevel(currentValue);

        reactionFX.SetSpawnDistribution(level);

        foreach (var post in posts)
            post.SetEngagementValue(level);

        UpdatePostLevelUI(level);
    }

    /// <summary>
    /// Updates the UI showing number of posts created.
    /// </summary>
    private void UpdatePostCountUI()
    {
        if (postCountText != null)
            postCountText.text = postCount.ToString();
    }

    /// <summary>
    /// Updates the UI showing engagement level (converted to int).
    /// </summary>
    private void UpdatePostLevelUI(EngagementLevel level)
    {
        if (postLevelText != null)
            postLevelText.text = "Lv." + ConvertLevelToInt(level).ToString();
    }

    #endregion

    #region === Helpers ===

    /// <summary>
    /// Converts engagement value (0.0–1.0) to engagement level tier.
    /// </summary>
    private EngagementLevel GetEngagementLevel(float value)
    {
        if (value <= .3f) return EngagementLevel.Low;
        if (value <= .6f) return EngagementLevel.Medium;
        if (value <= .9f) return EngagementLevel.High;
        return EngagementLevel.VeryHigh;
    }

    /// <summary>
    /// Returns money multiplier based on engagement level.
    /// </summary>
    private double GetMoneyBonusByEngagementLevel(EngagementLevel level) => level switch
    {
        EngagementLevel.Low => 0.5,
        EngagementLevel.Medium => 1.0,
        EngagementLevel.High => 1.5,
        EngagementLevel.VeryHigh => 2.0,
        _ => 0.0
    };

    /// <summary>
    /// Converts EngagementLevel enum to an int.
    /// Low=1, Medium=2, High=3, VeryHigh=4
    /// </summary>
    private int ConvertLevelToInt(EngagementLevel level) => level switch
    {
        EngagementLevel.Low => 1,
        EngagementLevel.Medium => 2,
        EngagementLevel.High => 3,
        EngagementLevel.VeryHigh => 4,
        _ => 0
    };

    #endregion

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
        {
            BeginCooldown();

            if (moneyRoutine == null)
                moneyRoutine = StartCoroutine(AutoAddMoney());
        }
    }
    #endregion
}
