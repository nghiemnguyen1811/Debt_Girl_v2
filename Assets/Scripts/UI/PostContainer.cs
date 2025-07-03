using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PostContainer : MonoBehaviour
{
    #region === UI References ===

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI captionText;
    [SerializeField] private Image postImage;

    [SerializeField] private TextMeshProUGUI likeCountText;
    [SerializeField] private TextMeshProUGUI commentCountText;
    [SerializeField] private TextMeshProUGUI shareCountText;

    #endregion

    #region === Interaction Settings ===

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 2f;
    [SerializeField] private Vector2 likeRange = new Vector2(200, 3000);
    [SerializeField] private Vector2 commentRange = new Vector2(20, 500);
    [SerializeField] private Vector2 shareRange = new Vector2(10, 200);

    #endregion

    #region === State ===

    [Header("Engagement Value (0–100)")]
    private EngagementLevel engagementValue = EngagementLevel.Medium;

    private int currentLikes;
    private int currentComments;
    private int currentShares;

    private int targetLikes;
    private int targetComments;
    private int targetShares;

    private Coroutine updateRoutine;

    #endregion

    #region === Unity Events ===

    // Start updating interactions if this post becomes active
    private void OnEnable()
    {
        if (updateRoutine == null)
            StartUpdatingInteractions();
    }

    #endregion

    #region === Configuration ===

    /// <summary>
    /// Configure the post with caption, image, and engagement level
    /// </summary>
    public void Configure(string caption, Sprite image, EngagementLevel engagementLevel)
    {
        engagementValue = engagementLevel;

        captionText.text = caption;
        postImage.sprite = image;

        // Set random target values for interaction counts
        targetLikes = Random.Range((int)likeRange.x, (int)likeRange.y);
        targetComments = Random.Range((int)commentRange.x, (int)commentRange.y);
        targetShares = Random.Range((int)shareRange.x, (int)shareRange.y);

        // Reset current values
        currentLikes = currentComments = currentShares = 0;

        likeCountText.text = "0";
        commentCountText.text = "0";
        shareCountText.text = "0";

        StartUpdatingInteractions();
    }

    /// <summary>
    /// Update engagement level dynamically during gameplay
    /// </summary>
    public void SetEngagementValue(EngagementLevel engagementLevel)
    {
        engagementValue = engagementLevel;
    }

    #endregion

    #region === Interaction Logic ===

    /// <summary>
    /// Starts or restarts the coroutine to simulate growing interaction values
    /// </summary>
    private void StartUpdatingInteractions()
    {
        if (updateRoutine != null)
            StopCoroutine(updateRoutine);

        updateRoutine = StartCoroutine(UpdateInteractionsLoop());
    }

    /// <summary>
    /// Coroutine that updates like, comment, and share counts every interval
    /// </summary>
    private IEnumerator UpdateInteractionsLoop()
    {
        while (true)
        {
            int likeDelta = GetLikeDeltaBasedOnValue(engagementValue);

            // Apply deltas with clamping to target max values
            currentLikes = Mathf.Clamp(currentLikes + likeDelta, 0, targetLikes);
            currentComments = Mathf.Clamp(currentComments + Random.Range(1, 10), 0, targetComments);
            currentShares = Mathf.Clamp(currentShares + Random.Range(1, 5), 0, targetShares);

            // Update the UI
            likeCountText.text = DoubleUtilities.ToIdleNotation(currentLikes);
            commentCountText.text = DoubleUtilities.ToIdleNotation(currentComments);
            shareCountText.text = DoubleUtilities.ToIdleNotation(currentShares);

            yield return new WaitForSeconds(updateInterval);
        }
    }

    /// <summary>
    /// Determines how fast likes increase or decrease based on engagement
    /// </summary>
    private int GetLikeDeltaBasedOnValue(EngagementLevel engagementLevel)
    {
        switch (engagementLevel)
        {
            case EngagementLevel.Low:
                return -Random.Range(5, 20); // Losing likes

            case EngagementLevel.Medium:
                return 0; // Stable

            case EngagementLevel.High:
                return Random.Range(5, 30); // Increasing

            case EngagementLevel.VeryHigh:
                return Random.Range(10, 60); // Increasing quickly

            default:
                Debug.Log("Unsupported engagement level");
                return 0;
        }
    }

    #endregion
}
