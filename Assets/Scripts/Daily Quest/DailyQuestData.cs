using UnityEngine;

[System.Serializable]
public class DailyQuestData
{
    // ─────────────────────────────────────────────────────
    // Quest Template Reference
    // ─────────────────────────────────────────────────────
    public DailyQuestDataSO questTemplate;

    // ─────────────────────────────────────────────────────
    // Quest Progress Data
    // ─────────────────────────────────────────────────────
    public int targetCount;
    public int currentCount;
    public bool isCompleted;

    // 🔹 Selected activity (only used when questType == Interact)
    public DailyActivity? selectedActivity;

    // ─────────────────────────────────────────────────────
    // Description Property
    // ─────────────────────────────────────────────────────
    // Returns the quest description formatted with target count (and activity if Interact).
    public string Description
    {
        get
        {
            if (questTemplate == null) return "???";

            string baseDesc = questTemplate.description;

            // Interact quest → has {1} placeholder for activity name
            if (questTemplate.questType == DailyQuestType.Interact)
            {
                string activityName = selectedActivity.HasValue
                    ? selectedActivity.Value.ToString()
                    : "Activity";

                return string.Format(baseDesc, targetCount, activityName);
            }

            // Non-interact quest with {0}
            if (baseDesc.Contains("{0}"))
                return string.Format(baseDesc, targetCount);

            // Default fallback
            return $"{baseDesc} ({targetCount} times)";
        }
    }

    // ─────────────────────────────────────────────────────
    // Quest Logic
    // ─────────────────────────────────────────────────────
    public bool CheckCompleted() => currentCount >= targetCount;

    public void AddProgress()
    {
        if (isCompleted) return;

        currentCount++;

        if (currentCount >= targetCount)
            isCompleted = true;
    }
}
