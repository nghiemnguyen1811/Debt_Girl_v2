using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoodManager : SingletonMonobehaviour<MoodManager>
{
    [Header(" Elements ")]
    [SerializeField] private PlayerControl playerControl;

    [Header("Mood Config")]
    [SerializeField] private List<MoodConditionDataSO> moodConditions;

    private Queue<MoodConditionType> moodQueue = new Queue<MoodConditionType>();

    private float totalDecayRate = 0f;
    private Coroutine decayCoroutine;

    private void Start()
    {
        if (playerControl == null || playerControl.stats == null)
        {
            Debug.LogError("[MoodManager] Thiếu PlayerControl hoặc PlayerStats!");
            enabled = false;
            return;
        }

        foreach (var mood in moodConditions)
            StartCoroutine(MoodTimerRoutine(mood));
    }

    private IEnumerator MoodTimerRoutine(MoodConditionDataSO moodData)
    {
        while (true)
        {
            float waitTime = Random.Range(moodData.minTime, moodData.maxTime);
            yield return new WaitForSeconds(waitTime);

            if (moodQueue.Contains(moodData.conditionType)) continue;

            moodQueue.Enqueue(moodData.conditionType);
            Debug.Log($"[MoodManager] Mood {moodData.conditionType} thêm vào queue.");

            // Nếu queue chỉ mới có 1 mood → set visual
            if (moodQueue.Count == 1)
                SetMoodVisual(moodData.conditionType);

            // Cộng decay rate vào tổng
            totalDecayRate += moodData.moodDecayRate;
            Debug.Log(totalDecayRate);
            Debug.Log($"[MoodManager] Tổng decay rate: {totalDecayRate}");

            // Nếu chưa có decay coroutine thì bật lên
            if (decayCoroutine == null)
                decayCoroutine = StartCoroutine(ApplyTotalMoodDecay());
        }
    }

    private void SetMoodVisual(MoodConditionType conditionType)
    {
        MoodConditionDataSO moodData = GetMoodData(conditionType);
        if (moodData == null) return;

        Debug.Log($"[MoodManager] Gán trạng thái mood: {moodData.name}");
        playerControl?.visualizer?.SetMoodVisual(moodData);
    }

    public void ClearMood(MoodConditionType conditionType)
    {
        if (moodQueue.Count == 0) return;

        Queue<MoodConditionType> tempQueue = new Queue<MoodConditionType>();

        while (moodQueue.Count > 0)
        {
            var mood = moodQueue.Dequeue();

            if (mood != conditionType)
                tempQueue.Enqueue(mood);

            else
            {
                var data = GetMoodData(mood);

                if (data != null)
                {
                    totalDecayRate -= data.moodDecayRate;
                    Debug.Log($"[MoodManager] Giảm decay rate: {data.moodDecayRate}. Tổng còn: {totalDecayRate}");
                }
            }
        }

        moodQueue = tempQueue;

        // Update visual
        if (moodQueue.Count > 0)
            SetMoodVisual(moodQueue.Peek());

        else
        {
            playerControl?.visualizer?.ClearMoodVisual();
            Debug.Log("[MoodManager] Không còn mood nào. Reset trạng thái visual.");
        }

        // Nếu tổng decay = 0 thì dừng coroutine
        if (totalDecayRate <= 0f && decayCoroutine != null)
        {
            StopCoroutine(decayCoroutine);
            decayCoroutine = null;
            SetMoodVisual(MoodConditionType.Normal);
            Debug.Log("[MoodManager] Dừng coroutine decay vì không còn mood nào.");
        }
    }

    private IEnumerator ApplyTotalMoodDecay()
    {
        Debug.Log("[MoodManager] Bắt đầu coroutine decay tổng.");

        while (true)
        {
            if (totalDecayRate > 0f)
                playerControl.stats.ApplyMoodChange(-totalDecayRate * Time.deltaTime);

            yield return null;
        }
    }

    public MoodConditionType? GetCurrentMoodInQueue()
    {
        return moodQueue.Count > 0 ? moodQueue.Peek() : (MoodConditionType?)null;
    }

    private MoodConditionDataSO GetMoodData(MoodConditionType type)
    {
        return moodConditions.Find(m => m.conditionType == type);
    }
}
