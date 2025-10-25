using UnityEngine;
using System;

/// <summary>
/// Stores runtime and progress data for a single daily quest.
/// </summary>
[System.Serializable]
public class DailyQuestData
{
    // ==================================================
    // ▶ QUEST REFERENCE
    // ==================================================
    public string questID;
    public DailyQuestDataSO questTemplate;

    // ==================================================
    // ▶ QUEST PROGRESS DATA
    // ==================================================
    public int targetCount;
    public int currentCount;
    public bool isCompleted;

    // ==================================================
    // ▶ INTERACT QUEST DATA
    // ==================================================
    public DailyActivity selectedActivity = DailyActivity.None;
    public int savedActivityInt;

    // ==================================================
    // ▶ DESCRIPTION PROPERTY
    // ==================================================
    /// <summary>
    /// Returns the formatted quest description based on its type and activity.
    /// </summary>
    public string Description
    {
        get
        {
            // 🧩 Safety check
            if (questTemplate == null)
                return "???";

            string baseDesc = questTemplate.description;

            // 🟩 Interact Quest → Use activity-specific description
            if (questTemplate.questType == DailyQuestType.Interact)
            {
                foreach (var req in questTemplate.activityRequirements)
                {
                    if (req.activity == selectedActivity)
                    {
                        string desc = string.IsNullOrEmpty(req.description)
                            ? baseDesc
                            : req.description;

                        // Replace {0} placeholder with target count
                        return desc.Contains("{0}")
                            ? string.Format(desc, targetCount)
                            : desc;
                    }
                }

                // Fallback: Replace {0} manually if not matched
                return baseDesc.Replace("{0}", targetCount.ToString());
            }

            // 🟩 Normal Quest → Use template description
            return baseDesc.Contains("{0}")
                ? string.Format(baseDesc, targetCount)
                : baseDesc;
        }
    }

    // ==================================================
    // ▶ QUEST LOGIC
    // ==================================================
    /// <summary>Increases quest progress by one and marks as complete if target reached.</summary>
    public void AddProgress()
    {
        if (isCompleted) return;

        currentCount++;
        if (currentCount >= targetCount)
            isCompleted = true;
    }
}
