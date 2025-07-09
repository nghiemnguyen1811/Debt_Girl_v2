using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages mood status effects that decay the player's mood over time,
/// including visual feedback and multiple stacked conditions.
/// </summary>
public class MoodManager : SingletonMonobehaviour<MoodManager>
{
    #region === Inspector Fields ===

    [Header(" Elements ")]
    [SerializeField] private PlayerControl playerControl;

    [Header(" Mood Config ")]
    [SerializeField] private List<MoodConditionDataSO> moodConditions;

    #endregion

    #region === Runtime State ===

    private Queue<MoodConditionType> moodQueue = new();
    private float totalDecayRate = 0f;
    private Coroutine decayCoroutine;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        if (playerControl == null || playerControl.stats == null)
        {
            Debug.LogError("[MoodManager] Missing PlayerControl or PlayerStats!");
            enabled = false;
            return;
        }

        foreach (var mood in moodConditions)
            StartCoroutine(MoodTimerRoutine(mood));
    }

    #endregion

    #region === Mood Timer and Decay Logic ===

    /// <summary>
    /// Coroutine that waits for a random interval and adds mood condition to the queue.
    /// </summary>
    private IEnumerator MoodTimerRoutine(MoodConditionDataSO moodData)
    {
        while (true)
        {
            float waitTime = Random.Range(moodData.minTime, moodData.maxTime);
            yield return new WaitForSeconds(waitTime);

            if (moodQueue.Contains(moodData.conditionType)) continue;

            moodQueue.Enqueue(moodData.conditionType);
            Debug.Log($"[MoodManager] Mood {moodData.conditionType} added to queue.");

            if (moodQueue.Count == 1)
                SetMoodVisual(moodData.conditionType);

            totalDecayRate += moodData.moodDecayRate;
            Debug.Log($"[MoodManager] Total decay rate: {totalDecayRate}");

            if (decayCoroutine == null)
                decayCoroutine = StartCoroutine(ApplyTotalMoodDecay());
        }
    }

    /// <summary>
    /// Continuously applies the total mood decay rate to the player's mood stat.
    /// </summary>
    private IEnumerator ApplyTotalMoodDecay()
    {
        Debug.Log("[MoodManager] Starting mood decay coroutine.");

        while (true)
        {
            if (totalDecayRate > 0f)
                playerControl.stats.ApplyStatChange(StatType.Mood, -totalDecayRate * Time.deltaTime);

            yield return null;
        }
    }

    #endregion

    #region === Mood Visual and Queue Management ===

    /// <summary>
    /// Sets the visual based on the current mood condition.
    /// </summary>
    private void SetMoodVisual(MoodConditionType conditionType)
    {
        MoodConditionDataSO moodData = GetMoodData(conditionType);
        if (moodData == null) return;

        Debug.Log($"[MoodManager] Setting mood visual: {moodData.name}");
        playerControl?.visualizer?.SetMoodVisual(moodData);
    }

    /// <summary>
    /// Clears a specific mood from the queue and updates the visual and decay rate.
    /// </summary>
    public void ClearMood(MoodConditionType conditionType)
    {
        if (moodQueue.Count == 0) return;

        Queue<MoodConditionType> tempQueue = new();
        while (moodQueue.Count > 0)
        {
            var mood = moodQueue.Dequeue();

            if (mood != conditionType)
            {
                tempQueue.Enqueue(mood);
                continue;
            }

            var data = GetMoodData(mood);
            if (data != null)
            {
                totalDecayRate -= data.moodDecayRate;
                Debug.Log($"[MoodManager] Decreased decay rate: {data.moodDecayRate}. New total: {totalDecayRate}");
            }
        }

        moodQueue = tempQueue;

        if (moodQueue.Count > 0)
            SetMoodVisual(moodQueue.Peek());

        else
        {
            playerControl?.visualizer?.ClearMoodVisual();
            Debug.Log("[MoodManager] No moods left. Resetting visual.");
        }

        if (totalDecayRate <= 0f && decayCoroutine != null)
        {
            StopCoroutine(decayCoroutine);
            decayCoroutine = null;
            SetMoodVisual(MoodConditionType.Normal);
            Debug.Log("[MoodManager] Stopped mood decay coroutine.");
        }
    }

    /// <summary>
    /// Returns the current mood on top of the queue (or null if none).
    /// </summary>
    public MoodConditionType? GetCurrentMoodInQueue()
    {
        return moodQueue.Count > 0 ? moodQueue.Peek() : (MoodConditionType?)null;
    }

    #endregion

    #region === Utility ===

    /// <summary>
    /// Finds the mood data scriptable object for the given type.
    /// </summary>
    private MoodConditionDataSO GetMoodData(MoodConditionType type)
    {
        return moodConditions.Find(m => m.conditionType == type);
    }

    #endregion
}
