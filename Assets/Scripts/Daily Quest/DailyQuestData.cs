using UnityEngine;
using Unity.VisualScripting;

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
    /// <summary>
    /// Returns the quest description formatted with target count (and activity description if Interact).
    /// </summary>
    public string Description
    {
        get
        {
            if (questTemplate == null)
                return "???";

            string baseDesc = questTemplate.description;

            // 🟩 Interact Quest: use specific activity description
            if (questTemplate.questType == DailyQuestType.Interact && selectedActivity.HasValue)
            {
                foreach (var req in questTemplate.activityRequirements)
                {
                    if (req.activity == selectedActivity.Value)
                    {
                        baseDesc = string.IsNullOrEmpty(req.description)
                            ? questTemplate.description
                            : req.description;

                        return baseDesc.Contains("{0}")
                            ? string.Format(baseDesc, targetCount)
                            : $"{baseDesc} ({targetCount} times)";
                    }
                }
            }

            // 🟩 Non-Interact Quest
            return baseDesc.Contains("{0}")
                ? string.Format(baseDesc, targetCount)
                : $"{baseDesc} ({targetCount} times)";
        }
    }

    // ─────────────────────────────────────────────────────
    // Quest Logic
    // ─────────────────────────────────────────────────────
    /// <summary>Returns true if quest progress reached target.</summary>
    public bool CheckCompleted() => currentCount >= targetCount;

    /// <summary>Adds progress by one and marks as completed if done.</summary>
    public void AddProgress()
    {
        if (isCompleted) return;

        currentCount++;
        if (currentCount >= targetCount)
            isCompleted = true;
    }
}
