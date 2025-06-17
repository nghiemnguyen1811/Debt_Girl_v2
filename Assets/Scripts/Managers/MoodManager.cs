using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoodManager : SingletonMonobehaviour<MoodManager>
{
    [Header(" Elements ")]
    [SerializeField] private PlayerControl playerControl;

    [Header("Mood Config")]
    [SerializeField] private List<MoodConditionDataSO> moodConditions;

    [SerializeField] private float minTime = 180f;
    [SerializeField] private float maxTime = 300f;

    private MoodConditionDataSO activeMood;
    private Coroutine moodDecayRoutine;
    private Coroutine moodLoopRoutine;

    private void Start()
    {
        if (playerControl == null)
            playerControl = GetComponent<PlayerControl>();

        if (playerControl == null || playerControl.stats == null)
        {
            Debug.LogError("[MoodManager] Thiếu PlayerControl hoặc PlayerStats!");
            enabled = false;
            return;
        }

        moodLoopRoutine = StartCoroutine(MoodLoop());
    }

    private IEnumerator MoodLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minTime, maxTime);
            yield return new WaitForSeconds(waitTime);

            if (activeMood != null || moodConditions.Count == 0)
                continue;

            activeMood = moodConditions[Random.Range(0, moodConditions.Count)];
            Debug.Log($"[MoodManager] Gán trạng thái mood: {activeMood.name}");

            // Gọi Visualizer để cập nhật icon và biểu cảm
            playerControl?.visualizer?.SetMoodVisual(activeMood);

            moodDecayRoutine = StartCoroutine(ApplyMoodDecay(activeMood));
        }
    }

    private IEnumerator ApplyMoodDecay(MoodConditionDataSO moodData)
    {
        while (activeMood == moodData)
        {
            playerControl.stats.ApplyMoodChange(-moodData.moodDecayRate * Time.deltaTime);
            yield return null;
        }
    }

    public void ClearMood()
    {
        if (activeMood == null) return;

        Debug.Log($"[MoodManager] Xóa trạng thái mood: {activeMood.name}");

        if (moodDecayRoutine != null)
            StopCoroutine(moodDecayRoutine);

        activeMood = null;
        moodDecayRoutine = null;

        // Reset biểu cảm nhân vật
        playerControl?.visualizer?.ClearMoodVisual();

        RestartMoodLoop();
    }

    private void RestartMoodLoop()
    {
        if (moodLoopRoutine != null)
            StopCoroutine(moodLoopRoutine);

        moodLoopRoutine = StartCoroutine(MoodLoop());
    }

    public MoodConditionDataSO GetActiveMood() => activeMood;
    public bool HasActiveMood() => activeMood != null;
}
